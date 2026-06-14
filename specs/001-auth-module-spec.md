# Módulo de Autenticação JWT

Este módulo é responsável por autenticar os usuários e emitir tokens JWT (JSON Web Tokens) seguros para autorizar o acesso aos endpoints protegidos da API do JuliusFinances.

*Nota: Toda a infraestrutura técnica (tratamento global de exceções, padrões de persistência com Value Converters, gerenciamento seguro de segredos e organização de rotas de Minimal APIs) deve seguir estritamente o definido na [Especificação de Arquitetura Global](000-global-architecture-spec.md).*

---

## 1. Fluxo de Autenticação

1. **Login:** O usuário envia suas credenciais (e-mail e senha) para o endpoint de autenticação.
2. **Validação:** O sistema valida as credenciais contra os dados registrados no PostgreSQL.
3. **Geração do Token:** Se as credenciais forem válidas, a API gera e retorna um token JWT contendo as claims do usuário e um tempo de expiração definido.
4. **Requisições Protegidas:** O cliente envia o token no cabeçalho `Authorization: Bearer <token>` para acessar recursos protegidos.

---

## 2. Configuração do Token

A validade do token JWT deve ser parametrizada em minutos no arquivo de configuração padrão do ASP.NET (`appsettings.json`), enquanto o segredo de assinatura deve residir estritamente em um ambiente seguro de segredos.

### Exemplo de Configuração (`appsettings.json` - Versionado)
```json
{
  "JwtSettings": {
    "Secret": "", 
    "ExpiryInMinutes": 60
  }
}
```

### Exemplo de Segredos Locais (`secrets.json` - Fora do Git)
```json
{
  "JwtSettings": {
    "Secret": "chave_secreta_super_segura_de_desenvolvimento_com_pelo_menos_32_caracteres"
  }
}
```

### Requisitos:
* O tempo de expiração do token gerado deve respeitar o valor definido na chave `ExpiryInMinutes`.
* **Sem Refresh Token:** Por simplicidade, a aplicação utilizará apenas o token de acesso normal (Access Token JWT) de curta/média duração, sem suporte a mecanismos de renovação automática via Refresh Token. Quando o token expirar, o usuário deverá realizar um novo login.

---

## 3. Entidade de Usuário (`User`) e Objetos de Valor

Toda a modelagem e nomenclatura de código (classes, propriedades, métodos) do módulo deve ser escrita em **inglês**, enquanto os comentários de código devem ser em **português (pt-BR)**.

Seguindo os princípios do **Object Calisthenics** (especialmente a regra de envelopar todos os primitivos e strings), os atributos da entidade `User` devem ser representados por **Objetos de Valor (Value Objects)** ricos, contendo suas próprias validações e comportamentos específicos e autônomos.

### Atributos (Objetos de Valor / Propriedades)
* **Id (`UserId`):** Um Objeto de Valor (Strongly Typed ID) que envelopa o identificador único (`Guid`).
* **Name (`Name`):** Objeto de Valor que encapsula e valida o nome do usuário.
* **Email (`Email`):** Objeto de Valor que valida o formato de e-mail e garante consistência.
* **Password (`Password`):** Objeto de Valor que encapsula a senha criptografada.
* **CreatedAt (`DateTime`):** Data e hora de criação do usuário (UTC).
* **UpdatedAt (`DateTime?`):** Data e hora da última atualização do usuário (UTC).

### Comportamentos dos Objetos de Valor (Value Objects)
* **`Name`:**
  * **Validação:** Não pode ser nulo ou vazio; deve conter tamanho mínimo de 3 caracteres e máximo de 150 caracteres.
  * **Comportamento:** Capitalização automática ou formatação para exibição.
* **`Email`:**
  * **Validação:** Deve validar se a string segue um formato válido de e-mail.
  * **Comportamento:** Conversão automática de todos os caracteres para letras minúsculas (normalização) para consistência no banco.
* **`Password`:**
  * **Validação de Complexidade:** Durante a criação de uma nova senha (a partir de texto limpo), deve-se validar requisitos mínimos de segurança (mínimo de 8 caracteres, pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial).
  * **Integridade:** O Objeto de Valor armazena apenas a string do hash gerado de forma segura e não armazena a senha original em texto limpo.
  * **Criptografia por Abstração:** O domínio não conhece o algoritmo de hashing (ex: BCrypt, Argon2id). Ele depende de uma interface abstrata `IPasswordHasher` injetada para gerar ou verificar o hash.

### Abstrações do Domínio (`JuliusFinances.Core`)
* **`IPasswordHasher` (Interface):**
  * `string Hash(string plainTextPassword)`: Gera um hash seguro a partir de uma senha válida.
  * `bool Verify(string plainTextPassword, string hashedPassword)`: Compara a senha em texto limpo com o hash armazenado.

### Comportamentos da Entidade `User` (Regras de Domínio)
* **Encapsulamento Total:** Todas as propriedades são somente leitura ou possuem `private set`, sendo modificadas exclusivamente via métodos de domínio.
* **Consistência Atômica:** O domínio delega a validação de formato e conteúdo para os respectivos Objetos de Valor, focando exclusivamente na integridade das regras de alto nível.
* **Métodos de Domínio:** Métodos como `UpdateProfile(Name name, Email email)` e `UpdatePassword(Password password)` garantem que o estado do usuário seja alterado de forma válida e atualizam de forma segura a propriedade `UpdatedAt`.

---

## 4. Endpoints de Autenticação (Minimal APIs)

Seguindo as diretrizes de injeção de dependência e organização de rotas descritas na [Especificação de Arquitetura Global](000-global-architecture-spec.md), as rotas devem ser agrupadas (`/auth`) e implementadas em uma classe separada (ex: `AuthEndpoints`).

### 4.1. Registro de Usuário (`POST /auth/register`)
Responsável pela criação de novas contas de usuário.

* **Contrato de Entrada (Request Body):**
  ```json
  {
    "name": "Nome do Usuário",
    "email": "usuario@exemplo.com",
    "password": "SenhaSegura123!"
  }
  ```
* **Comportamento & Validações:**
  1. Valida as regras de força da senha em texto limpo. Caso inválida, dispara uma `DomainException`.
  2. Instancia os Value Objects `Name` e `Email`. Qualquer erro de validação de domínio disparará uma `DomainException` (HTTP `400 Bad Request`).
  3. Verifica se o e-mail informado já está cadastrado no banco de dados. Caso sim, lança uma `EmailAlreadyExistsException` (que herda de `DomainException` e será mapeada para o HTTP `409 Conflict`).
  4. Criptografa a senha usando o `IPasswordHasher` (implementação de infraestrutura baseada em Argon2id ou BCrypt) e instancia o Value Object `Password`.
  5. Salva a entidade `User` no PostgreSQL através do EF Core (utilizando os Value Converters globais).
* **Contrato de Saída (Response HTTP 201 Created):**
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Nome do Usuário",
    "email": "usuario@exemplo.com"
  }
  ```

### 4.2. Login / Geração de Token (`POST /auth/login`)
Responsável por validar as credenciais e emitir o token de acesso.

* **Contrato de Entrada (Request Body):**
  ```json
  {
    "email": "usuario@exemplo.com",
    "password": "SenhaSegura123!"
  }
  ```
* **Comportamento & Validações:**
  1. Instancia o Value Object `Email` para normalização automática e validação de formato.
  2. Busca o usuário no PostgreSQL com o e-mail correspondente.
  3. Se o usuário não existir ou se a verificação da senha via `IPasswordHasher.Verify(password, user.Password.Value)` falhar, lança uma `DomainException` genérica (HTTP `400 Bad Request`) com uma mensagem segura (ex: `"E-mail ou senha incorretos."`) para evitar a descoberta de e-mails cadastrados.
  4. Gera o token JWT seguro com as claims do usuário e a validade parametrizada em minutos obtida do arquivo de configuração da API.
* **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresInMinutes": 60,
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Nome do Usuário",
      "email": "usuario@exemplo.com"
    }
  }
  ```
