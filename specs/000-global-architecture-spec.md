# Especificação de Arquitetura Global

Este documento estabelece as diretrizes arquiteturais, padrões de design e práticas de desenvolvimento para todo o ecossistema do JuliusFinances. Todas as especificações de módulos específicos devem seguir estas definições globais para garantir consistência e reuso de código.

---

## 1. Estrutura do Projeto (Clean Architecture Compacta)

Visando manter a simplicidade e agilidade de desenvolvimento sem abrir mão do desacoplamento, a solução JuliusFinances adota uma **Arquitetura Limpa Compacta em duas camadas de código**:

1. **`JuliusFinances.Core` (Domain + Application Layers):**
   * Contém as entidades de domínio, objetos de valor (Value Objects), exceções de domínio e regras de negócio centrais.
   * Contém os contratos e interfaces (ex: interfaces de repositórios, serviços, etc.) e os Casos de Uso/Serviços de Aplicação.
   * **Restrição:** É uma biblioteca de classes totalmente pura. Não possui dependência direta de nenhum framework externo, banco de dados ou infraestrutura de entrega (ex: ASP.NET Core, EF Core).
2. **`JuliusFinances.Api` (Presentation + Infrastructure Layers):**
   * Contém os controladores e rotas de entrega (Minimal APIs).
   * Contém os detalhes de infraestrutura (Entity Framework Core DbContext, Migrações, implementações concretas de Repositórios, hashing de senhas, etc.).
   * Depende diretamente do `JuliusFinances.Core`.

---

## 2. Padrão de Persistência (EF Core + DDD + Object Calisthenics)

Para viabilizar o uso das práticas do **Object Calisthenics** (especialmente a regra de envelopar primitivos em Objetos de Valor):

### Mapeamento de Objetos de Valor (Value Objects)
* **Objetos de Valor de Propriedade Única:** Todos os VOs que envelopam um único primitivo (ex: `Email`, `Name`, `UserId`) devem ser mapeados no EF Core utilizando **Value Converters** (`HasConversion`). Isso converte o VO de/para seu tipo nativo correspondente no PostgreSQL (ex: `varchar`, `uuid`) de forma transparente.
* **Objetos de Valor Compostos:** VOs que encapsulam múltiplas propriedades (ex: um futuro VO `Money` contendo `Amount` e `Currency`) devem ser mapeados utilizando o recurso de **Owned Entity Types** (`OwnsOne`) do EF Core, garantindo o armazenamento correto em colunas na mesma tabela da entidade pai.

### Convenções de Banco de Dados Automatizadas (PostgreSQL)
* Para garantir consistência com o dialeto PostgreSQL, todas as tabelas e colunas devem seguir a nomenclatura **snake_case**.
* Essa convenção deve ser automatizada globalmente através do pacote NuGet `EFCore.NamingConventions` chamando `.UseSnakeCaseNamingConvention()` na configuração do `DbContext` da API.

---

## 3. Tratamento Global de Erros (RFC 7807 - Problem Details)

O sistema deve adotar uma abordagem centralizada e padronizada utilizando os recursos nativos modernos do .NET 10.

### Exceções de Domínio (`DomainException`)
* Deve existir uma classe de exceção base chamada `DomainException` (herdando de `Exception`).
* Violações de regras de negócio ou validações falhas dentro de Objetos de Valor e Entidades devem lançar instâncias de `DomainException` (ou suas subclasses específicas).

### Pipeline de Exceções Nativo com `IExceptionHandler`
* A API deve implementar e registrar uma classe que implementa a interface nativa **`IExceptionHandler`** (introduzida a partir do .NET 8).
* Este handler capturará as exceções lançadas no pipeline e retornará respostas padronizadas sob o formato **RFC 7807 (Problem Details)**:
  * **Exceções de Domínio de Conflito** (ex: tentativa de registro de um e-mail já existente): Devem retornar HTTP `409 Conflict`.
  * **Exceções de Domínio de Regra Geral / Validação** (ex: e-mail em formato inválido, nome curto): Devem retornar HTTP `400 Bad Request`.
  * **Exceções Inesperadas** (erros internos de infraestrutura): Devem retornar HTTP `500 Internal Server Error`, gravando o log detalhado internamente e exibindo uma mensagem limpa e segura ao cliente final.

---

## 4. Gerenciamento Seguro de Configurações e Segredos

### Proibição Absoluta de Segredos no Git
* É terminantemente proibido commitar chaves secretas de assinatura (como a chave secreta do JWT), senhas de banco de dados ou tokens de serviços externos em arquivos de configuração versionados (`appsettings.json`, `appsettings.Development.json`).

### Práticas de Segurança e Validação no Startup
* **Desenvolvimento:** Uso obrigatório da ferramenta **User Secrets** do .NET (`dotnet user-secrets`) para armazenar chaves privadas e credenciais locais.
* **Produção / Homologação:** Utilização exclusiva de **Variáveis de Ambiente** de sistema ou cofres de segredos.
* **Validação Ativa (Fail-Fast):** As configurações sensíveis injetadas no container da API (ex: JwtSettings) devem ser validadas em tempo de inicialização utilizando **Options Validation** com o método `.ValidateOnStart()`. Se alguma chave crítica estiver ausente ou inválida, a aplicação deve falhar no startup impedindo o funcionamento do container instável.

---

## 5. Estrutura de Organização de Rotas (Minimal APIs)

### Endpoint Groups e Mapeamento Limpo
* Cada módulo funcional deve expor e organizar suas rotas usando o recurso de **Route Groups** (`MapGroup`) do ASP.NET Core.
* A definição dos endpoints deve ser modularizada em classes específicas de extensão (ex: `AuthEndpoints`, `TransactionEndpoints`), em vez de serem declarados todos diretamente na raiz do `Program.cs`.
* Injeções de dependência de serviços do ASP.NET devem ser resolvidas diretamente como parâmetros nos métodos dos endpoints.

---

## 6. Padrões de Projeto e Qualidade de Código (S.O.L.I.D.)

O desenvolvimento do JuliusFinances deve ser guiado por princípios de S.O.L.I.D. para garantir facilidade de manutenção, testabilidade e desacoplamento:

* **S - Single Responsibility (Responsabilidade Única):** Cada classe deve ter apenas uma razão para mudar. Evitar classes "Deus" (God Objects) que acumulam múltiplas responsabilidades (ex: uma entidade que também gerencia conexões de rede ou persistência).
* **O - Open/Closed (Aberto/Fechado):** O código deve ser aberto para extensão, mas fechado para modificação. Novas funcionalidades devem ser adicionadas estendendo o comportamento através de abstrações (interfaces/polimorfismo), sem alterar código estável existente.
* **L - Liskov Substitution (Substituição de Liskov):** Subclasses devem poder substituir suas superclasses perfeitamente sem alterar o comportamento esperado do sistema.
* **I - Interface Segregation (Segregação de Interfaces):** Interfaces devem ser específicas e focadas. É melhor ter várias interfaces pequenas e coesas do que uma única interface genérica que obrigue os clientes a implementar métodos desnecessários.
* **D - Dependency Inversion (Inversão de Dependência):** Módulos de alto nível não devem depender de módulos de baixo nível; ambos devem depender de abstrações. Classes concretas devem depender de interfaces, facilitando a injeção de dependências e a criação de mocks em testes automatizados.

---

## 7. Configurações de Rede, Escuta de Endpoints (Bindings) e CORS

Para garantir que a API seja acessível para outros dispositivos na rede doméstica/local, bem como em futuros deploys em ambientes conteinerizados (Docker/Kubernetes) ou servidores de Homelab, as seguintes diretrizes de infraestrutura de rede são obrigatórias:

### 7.1. Precedência de Configurações do Kestrel
No ecossistema ASP.NET Core, há uma ordem estrita de precedência sobre as definições de portas e URLs de escuta:
1. **Configurações em arquivos JSON (`appsettings.json`, `appsettings.Development.json`):** A chave `"Kestrel"` definida nestes arquivos possui **prioridade absoluta** e sobrescreve tanto o arquivo `launchSettings.json` quanto parâmetros passados por linha de comando (`--urls`).
2. **Parâmetros de linha de comando (`--urls` ou `--port`):** Configurações passadas no startup do binário ou comando de execução.
3. **Arquivo de perfis de inicialização (`Properties/launchSettings.json`):** Lido apenas pelo ferramental CLI de desenvolvimento (`dotnet run`, IDEs).
4. **Variáveis de Ambiente (`ASPNETCORE_URLS`):** Sobrescreve as configurações de menor prioridade.

### 7.2. Regra de Ligação Global (Universal Bindings)
* É terminantemente proibido manter o valor de escuta padrão `localhost` ou `127.0.0.1` de forma fixa nas chaves `"Kestrel"` dos arquivos `appsettings.*.json` do projeto, uma vez que isso restringe a API apenas ao loopback local impedindo conexões remotas.
* A configuração de escuta em desenvolvimento e produção deve utilizar o caractere curinga **`*`** (ex: `http://*:5290` ou `https://*:7085`), forçando a API a escutar em todas as interfaces de rede IPv4 e IPv6 disponíveis na máquina física ou container.

### 7.3. Política de CORS (Cross-Origin Resource Sharing)
* Com a API acessível remotamente na rede local, clientes Web (frontend executando em navegadores de outras máquinas da rede local) sofrerão bloqueio de requisições de origem cruzada.
* O `Program.cs` deve registrar de forma explícita o serviço de CORS e ativar o middleware correspondente (`app.UseCors()`) antes de habilitar autenticação e autorização (`app.UseAuthentication()` e `app.UseAuthorization()`). Em ambientes de desenvolvimento local, a política de CORS deve ser flexível, permitindo qualquer origem (`AllowAnyOrigin()`), método e cabeçalho para garantir facilidade de testes.
