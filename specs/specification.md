# Especificação do Projeto: JuliusFinances

JuliusFinances é um sistema de controle financeiro pessoal em desenvolvimento utilizando .NET 10 e PostgreSQL.

---

## 1. Tecnologias e Estrutura Atual da Solução

A solução está estruturada em três projetos principais com suporte a banco de dados em ambiente de desenvolvimento:

* **JuliusFinances.Api:** Web API utilizando ASP.NET Core Minimal APIs (.NET 10). Contém um endpoint básico de teste (`GET /` retornando `"Hello World!"`), está configurada com suporte a banco de dados PostgreSQL e está organizada sob camadas de Apresentação e Infraestrutura estruturadas por módulos:
  ```text
  JuliusFinances.Api/
  ├── Common/                      # Infraestrutura e utilitários globais
  │   ├── Database/                # DbContext único global (JuliusDbContext)
  │   ├── Errors/                  # Handlers de erro global (IExceptionHandler - RFC 7807)
  │   └── Security/                # Implementações globais de segurança (JWT e Criptografia)
  └── Modules/                     # Divisão do sistema por módulos de negócio
      └── Auth/                    # Módulo de Autenticação JWT
          ├── Presentation/        # Camada de Entrega / Rotas (AuthEndpoints)
          └── Infrastructure/      # Detalhes Técnicos e Persistência específicos do módulo
              ├── Persistence/     # Repositórios e configurações de banco do módulo
              │   └── Configurations/ # Configurações de mapeamento de Entidade (UserConfiguration)
              └── Services/        # Serviços utilitários específicos (ex: JwtTokenGenerator)
  ```
* **JuliusFinances.Core:** Camada de biblioteca de classes (.NET 10) que contém as regras de negócio de Domínio e Aplicação, estruturada por módulos (Arquitetura Limpa Compacta):
  ```text
  JuliusFinances.Core/
  ├── Common/                      # Recursos globais compartilhados do Core
  │   ├── Domain/                  # Exceções de domínio ou utilitários globais
  │   └── Security/                # Interfaces de segurança (ex: IPasswordHasher)
  └── Modules/                     # Divisão do sistema por módulos de negócio
      └── Auth/                    # Módulo de Autenticação JWT
          ├── Domain/              # Regras de Negócio e Modelagem
          │   ├── Entities/        # Entidades de Domínio ricas (User)
          │   ├── ValueObjects/    # Objetos de Valor (UserId, Name, Email, Password)
          │   └── Exceptions/      # Exceções específicas do módulo (EmailAlreadyExistsException)
          └── Application/         # Casos de Uso e Orquestração
              ├── Interfaces/      # Contratos para infraestrutura (IUserRepository)
              └── UseCases/        # Casos de Uso do módulo
                  ├── RegisterUser/# Caso de uso para registro de usuário
                  └── Login/       # Caso de uso para login e geração de token
  ```
* **JuliusFinances.Tests:** Projeto de testes automatizados (.NET 10) utilizando xUnit. Atualmente contém uma estrutura inicial de teste vazia.

### Pacotes Instalados (no projeto JuliusFinances.Api)
* `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.2)
* `Microsoft.EntityFrameworkCore.Design` (v10.0.9)

---

## 2. Infraestrutura e Bancos de Dados por Ambiente

A aplicação está configurada para suportar dois ambientes distintos (Desenvolvimento e Produção), operando com strings de conexão e portas HTTP/HTTPS separadas.

### 2.1. Ambiente de Desenvolvimento (Development)
* **Banco de Dados:** `julius_finances_db_dev`
* **Usuário:** `postgres`
* **Senha:** `postgres`
* **String de Conexão:** `Host=localhost;Port=5432;Database=julius_finances_db_dev;Username=postgres;Password=postgres`
* **Portas da API:** Escuta universal em `http://*:5290` e `https://*:7085` (disponível para outros computadores da rede local)
* **Configurações:** Definidas no arquivo `JuliusFinances.Api/appsettings.Development.json` e ativas sob o perfil de inicialização `http` ou `https`.

### 2.2. Ambiente de Produção (Production)
* **Banco de Dados:** `julius_finances_db_prod`
* **Usuário:** `postgres`
* **Senha:** `postgres`
* **String de Conexão:** `Host=localhost;Port=5432;Database=julius_finances_db_prod;Username=postgres;Password=postgres`
* **Portas da API:** Escuta universal em `http://*:5291` e `https://*:7086` (disponível para outros computadores da rede local)
* **Configurações:** Definidas no arquivo `JuliusFinances.Api/appsettings.Production.json` e ativas sob o perfil de inicialização `production`.

### 2.3. Acessibilidade na Rede Local e CORS
A API está configurada para escutar em todas as interfaces de rede (`*` ou `0.0.0.0`), permitindo que outros dispositivos na rede doméstica acessem o sistema diretamente pelo endereço IP local do servidor (ex: `http://<IP-DO-HOMELAB>:5290`). 
Para que navegadores de outros computadores acessem os endpoints de forma bem-sucedida a partir de um cliente frontend separado, a aplicação possui o middleware de **CORS** habilitado no `Program.cs` (`app.UseCors()`), liberando origens cruzadas, cabeçalhos e métodos de forma flexível em ambiente de desenvolvimento.

### 2.4. Execução Automática de Migrações (EF Core)
Ao iniciar a aplicação em qualquer um dos ambientes, uma rotina de inicialização automática no `Program.cs` aplica as migrações pendentes no banco de dados do respectivo ambiente ativo usando `dbContext.Database.Migrate()`.

---

## 3. Como Executar a Solução

Assegure-se de que o seu servidor PostgreSQL local (na porta 5432) esteja rodando e com o usuário superuser `postgres` criado com a senha `postgres`.

### 3.1. Executar a API em Ambiente de Desenvolvimento (DEV)
Para iniciar a API utilizando a porta `5290` e o banco de desenvolvimento `julius_finances_db_dev`:
```bash
dotnet run --project JuliusFinances.Api/JuliusFinances.Api.csproj --launch-profile http
```

### 3.2. Executar a API em Ambiente de Produção (PROD)
Para iniciar a API utilizando a porta `5291` e o banco de produção `julius_finances_db_prod`:
```bash
dotnet run --project JuliusFinances.Api/JuliusFinances.Api.csproj --launch-profile production
```

### 3.3. Rodar os Testes Automatizados
```bash
dotnet test
```
