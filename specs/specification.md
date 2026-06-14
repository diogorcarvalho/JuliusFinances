# Especificação do Projeto: JuliusFinances

JuliusFinances é um sistema de controle financeiro pessoal em desenvolvimento utilizando .NET 10 e PostgreSQL.

---

## 1. Tecnologias e Estrutura Atual da Solução

A solução está estruturada em três projetos principais com suporte a banco de dados em ambiente de desenvolvimento:

* **JuliusFinances.Api:** Web API utilizando ASP.NET Core Minimal APIs (.NET 10). Contém um endpoint básico de teste (`GET /` retornando `"Hello World!"`) e está configurada com suporte a banco de dados PostgreSQL.
* **JuliusFinances.Core:** Camada de biblioteca de classes (.NET 10). Atualmente contém a estrutura básica inicial sem regras ou entidades definidas.
* **JuliusFinances.Tests:** Projeto de testes automatizados (.NET 10) utilizando xUnit. Atualmente contém uma estrutura inicial de teste vazia.

### Pacotes Instalados (no projeto JuliusFinances.Api)
* `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.2)
* `Microsoft.EntityFrameworkCore.Design` (v10.0.9)

---

## 2. Infraestrutura e Banco de Dados (Desenvolvimento)

### Docker (PostgreSQL)
Um banco de dados PostgreSQL 17 (imagem alpine) está configurado e em execução através do arquivo `docker-compose.yml` na raiz do projeto com os seguintes parâmetros:
* **Container Name:** `julius_postgres`
* **Porta:** `5432:5432`
* **Usuário:** `julius_user`
* **Banco:** `julius_db`
* **Senha:** `julius_secure_password_2026`

### Conexão no .NET
A string de conexão ativa está configurada em `JuliusFinances.Api/appsettings.Development.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=julius_db;Username=julius_user;Password=julius_secure_password_2026"
}
```

---

## 3. Como Executar a Solução Atual

1. **Subir o banco de dados (Docker):**
   ```bash
   docker compose up -d
   ```
2. **Executar a API (Modo Desenvolvimento):**
   ```bash
   dotnet run --project JuliusFinances.Api/JuliusFinances.Api.csproj
   ```
3. **Rodar os Testes:**
   ```bash
   dotnet test
   ```
