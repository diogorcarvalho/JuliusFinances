# 300-homelab-local-pipeline-spec.md

# Especificação de Infraestrutura: Pipeline de Produção e Deploy 100% Local (Homelab Debian)

Este documento especifica a infraestrutura de conteinerização, a arquitetura de rede interna e o script de automação de CI/CD para o ambiente de produção do **JuliusFinances**. Todo o ciclo de validação, build e deploy é executado localmente de forma isolada dentro do servidor host Debian, aproveitando o ecossistema de desenvolvimento remoto via SSH.

---

## 1. Topologia de Rede e Integração Contida

Diferente do ambiente de desenvolvimento que roda diretamente no host ou em portas expostas de teste, o ecossistema de produção opera de forma puramente conteinerizada e se acopla ao container de banco de dados PostgreSQL já existente no Homelab.

### 1.1. Resolução de Nomes e Isolamento de Bancos
* **Rede Externa do Docker (`julius-network`):** Os containers de produção da API e do Frontend Web associam-se à rede integrada que o banco de dados existente já utiliza.
* **Isolamento de Contexto:** A API de desenvolvimento continuará consumindo o banco `julius_finances_db_dev`. A infraestrutura de produção especificada neste documento consumirá exclusivamente o banco `julius_finances_db_prod` hospedado na mesma instância PostgreSQL.

## 2. Conteinerização do Backend (`JuliusFinances.Api/Dockerfile`)

A API adota a estratégia de **Multi-stage Build** para garantir que ferramentas pesadas de compilação do SDK do .NET 10 fiquem restritas ao estágio temporário de compilação, gerando uma imagem de runtime final extremamente leve e segura.

```dockerfile
# Estágio 1: Compilação e Publicação Otimizada
FROM [mcr.microsoft.com/dotnet/sdk:10.0](https://mcr.microsoft.com/dotnet/sdk:10.0) AS build
WORKDIR /src

# Copiar arquivos de projeto de forma isolada para maximizar cache de camadas do Docker
COPY ["JuliusFinances.Core/JuliusFinances.Core.csproj", "JuliusFinances.Core/"]
COPY ["JuliusFinances.Api/JuliusFinances.Api.csproj", "JuliusFinances.Api/"]
RUN dotnet restore "JuliusFinances.Api/JuliusFinances.Api.csproj"

# Copiar os arquivos remanescentes do código fonte
COPY . .
WORKDIR "/src/JuliusFinances.Api"

# Publicar binários otimizados em modo Release limpando telemetrias
RUN dotnet publish "JuliusFinances.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 2: Runtime enxuto de Produção
FROM [mcr.microsoft.com/dotnet/aspnet:10.0](https://mcr.microsoft.com/dotnet/aspnet:10.0) AS final
WORKDIR /app
COPY --from=build /app/publish .

# Vinculação universal de portas conforme a especificação de arquitetura global
EXPOSE 5291
ENV ASPNETCORE_URLS=http://*:5291

ENTRYPOINT ["dotnet", "JuliusFinances.Api.dll"]

---

## 3. Conteinerização do Frontend (`JuliusFinances.Web/Dockerfile`)

O frontend abandona o servidor Node/Vite de desenvolvimento e passa por uma compilação estática, sendo servido por um servidor web industrial **Nginx Alpine**.

```dockerfile
# Estágio 1: Build da SPA React
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Injeção de variáveis de ambiente em tempo de build para a Single Page Application
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_ENVIRONMENT=production

RUN npm run build

# Estágio 2: Distribuição com Nginx
FROM nginx:1.27-alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

```

### 3.1. Configuração do Roteador Web (`JuliusFinances.Web/nginx.conf`)

Para garantir a resiliência do cliente e impedir erros de `404 Not Found` ao recarregar a página em rotas privadas, o Nginx é configurado com um fallback nativo direcionando requisições para o ponto de entrada único:

```nginx
server {
    listen 80;
    server_name localhost;

    location / {
        root /usr/share/nginx/html;
        index index.html index.htm;
        try_files $uri$uri/ /index.html;
    }

    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}

```

## 4. Orquestração de Runtime (`docker-compose.prod.yml`)

Este arquivo gerencia estritamente o ecossistema de produção, operando de maneira completamente independente do ambiente de desenvolvimento.

```yaml
version: '3.8'

services:
  julius-api-prod:
    container_name: julius-finances-api-prod
    build:
      context: .
      dockerfile: JuliusFinances.Api/Dockerfile
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # Conexão amarrada de forma exclusiva ao banco de dados produtivo (segredos carregados do .env do host)
      - ConnectionStrings__DefaultConnection=Host=julius-postgres-db;Port=5432;Database=julius_finances_db_prod;Username=postgres;Password=${PROD_DB_PASSWORD}
      - JwtSettings__Secret=${PROD_JWT_SECRET}
      - JwtSettings__ExpiryInMinutes=60
      - EnableSwagger=true
    ports:
      - "5291:5291"
    networks:
      - julius-network
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    healthcheck:
      test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:5291/swagger/index.html || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 15s

  julius-web-prod:
    container_name: julius-finances-web-prod
    build:
      context: ./JuliusFinances.Web
      dockerfile: Dockerfile
      args:
        # IP estático do servidor Debian na sua rede local doméstica (porta ajustada para 5291)
        - VITE_API_URL=http://192.168.X.X:5291
    restart: always
    ports:
      - "8080:80"
    networks:
      - julius-network
    depends_on:
      julius-api-prod:
        condition: service_healthy
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    healthcheck:
      test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 5s

networks:
  julius-network:
    external: true

```

### 4.1. Configuração do Arquivo de Variáveis de Ambiente (`.env`)

Para evitar o vazamento de credenciais e segredos no repositório de controle de versão (Git), deve ser criado um arquivo `.env` de forma manual diretamente no diretório raiz do deploy do Homelab Debian. O Docker Compose carregará essas variáveis dinamicamente:

```env
# .env - CONFIGURAÇÃO DE SEGREDOS DE PRODUÇÃO (Não versionar!)
PROD_DB_PASSWORD=SUA_SENHA_SECRETA_DO_BANCO_AQUI
PROD_JWT_SECRET=UMA_CHAVE_SUPER_SECRETA_DE_PRODUCAO_COM_MAIS_DE_32_CARACTERES_PROD
```

---

## 5. Script de Automação de Pipeline (`deploy-prod.sh`)

Script em Bash executado diretamente de dentro do seu terminal SSH que orquestra e garante a integridade do ciclo completo de CI/CD (Testar, Backup, Derrubar, Reconstruir, Validar e Limpar).

```bash
#!/bin/bash

# Interromper a execução do script caso algum comando falhe (Fail-Fast)
set -e

# Configurações de IP local do Homelab
IP_HOMELAB="192.168.15.25"

echo "🚀 Iniciando a Pipeline Local de Produção - JuliusFinances..."

# 1. Garantia de Qualidade de Código (CI)
echo "🧪 Executando suite de testes automatizados locais..."
dotnet test JuliusFinances.Tests/JuliusFinances.Tests.csproj -c Release
echo "✓ Testes aprovados de forma bem-sucedida!"

# 2. Backup Preventivo de Banco de Dados de Produção
echo "🗄️ Executando backup de segurança preventiva do banco de dados..."
mkdir -p ./backups
# Tenta realizar o dump apenas se o container de banco estiver online
if docker ps --format '{{.Names}}' | grep -q "^julius-postgres-db$"; then
  docker exec julius-postgres-db pg_dump -U postgres -d julius_finances_db_prod > ./backups/prod_backup_$(date +%Y%m%d_%H%M%S).sql || echo "⚠️ Aviso: Falha ao gerar o dump do banco, prosseguindo com o deploy..."
else
  echo "⚠️ Aviso: Container de banco 'julius-postgres-db' não encontrado ativo. Pulando backup..."
fi

# 3. Orquestração de Estado Antigo
echo "🔄 Removendo containers de produção antigos ativos..."
docker compose -f docker-compose.prod.yml down --remove-orphans

# 4. Compilação e Deploy Conteinerizado (CD)
echo "📦 Compilando imagens Docker e inicializando novos containers de produção..."
VITE_API_URL="http://$IP_HOMELAB:5291" docker compose -f docker-compose.prod.yml up -d --build

# 5. Validação Ativa de Saúde (Post-deploy healthcheck validation)
echo "⏳ Aguardando e validando saúde do deploy da API..."
sleep 10
# Tenta fazer o ping HTTP na rota do Swagger
if curl -s -f -o /dev/null "http://$IP_HOMELAB:5291/swagger/index.html"; then
  echo "✓ API de Produção está online e saudável!"
else
  echo "❌ ERRO: A API de Produção falhou na inicialização pós-deploy!"
  echo "Logs da API:"
  docker compose -f docker-compose.prod.yml logs julius-api-prod
  exit 1
fi

# 6. Zeladoria e Gestão de Recursos do Homelab
echo "🧹 Efetuando limpeza de imagens órfãs e caches antigos de compilação..."
docker image prune -f

echo "🎉 Pipeline executada com sucesso absoluto!"
echo "📱 Interface Web Produção: http://$IP_HOMELAB:8080"
echo "⚙️  API de Produção: http://$IP_HOMELAB:5291"

```