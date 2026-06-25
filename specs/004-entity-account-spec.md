# Módulo de Configuração de Finanças - Entidade Conta

Este módulo (Finances Setup) é responsável por gerenciar as contas financeiras (Account) no JuliusFinances. As contas são as carteiras, contas bancárias, poupanças ou investimentos que sustentam e rastreiam os saldos e as transações do usuário. Diferente das categorias, o sistema adota um modelo estrito de isolamento de dados por usuário, no qual não existem contas globais de sistema; todas as contas pertencem obrigatoriamente a um usuário autenticado.

*Nota: Toda a infraestrutura técnica (tratamento global de exceções, padrões de persistência com Value Converters, gerenciamento seguro de segredos e organização de rotas de Minimal APIs) deve seguir estritamente o definido na [Especificação de Arquitetura Global](000-global-architecture-spec.md).*

---

## 1. Regras de Negócio e Comportamento

### 1.1. Visibilidade, Isolamento e Multi-inquilinato (Multi-tenant)
- **Contas Pessoais:** Todas as contas no sistema são privadas, individuais e pertencem obrigatoriamente a um usuário autenticado (`OwnerId` obrigatório e não-nulo). O Usuário A nunca poderá visualizar, utilizar, editar ou excluir as contas do Usuário B. Não existem contas globais compartilhadas ou visíveis para múltiplos usuários.

### 1.2. Regra de Listagem (Leitura)
- Ao solicitar a listagem de contas, o sistema deve retornar exclusivamente as contas pertencentes ao usuário autenticado.
- **Filtro de busca:** O repositório deve buscar todas as contas ativas (`IsDeleted == false`) cujo `OwnerId` (Identificador do Usuário) seja igual ao ID do usuário autenticado.

### 1.3. Regras para Criação (Escrita)
- Toda e qualquer conta criada via API por meio de requisições de clientes é classificada como Conta Pessoal, sendo obrigatoriamente associada ao ID do usuário autenticado (propriedade `OwnerId`).
- **Validação de Duplicidade:** O sistema não permite que o mesmo usuário possua duas contas ativas com o mesmo nome (comparação insensível a maiúsculas/minúsculas, espaços extras e acentuações/diacríticos).
  - O Usuário A não pode criar duas contas ativas chamadas "Itaú".
  - O Usuário A pode criar uma conta "Itaú", e o Usuário B também pode criar "Itaú" de forma independente.
  - Se o usuário possuir uma conta inativa (arquivada/soft-deleted), ele **pode** criar uma nova conta ativa com o mesmo nome.
- **Consistência de Unicidade no Banco de Dados:** Para evitar condições de corrida (ex: cliques duplos rápidos no frontend), deve ser configurado um índice único filtrado (parcial) no PostgreSQL:
  ```sql
  CREATE UNIQUE INDEX idx_accounts_owner_name_active ON accounts(owner_id, name) WHERE is_deleted = false;
  ```
- **Validação do Saldo Inicial para Dinheiro em Espécie:** Contas do tipo `Cash` (Dinheiro em Espécie / Carteira Física) representam recursos físicos de bolso e não possuem limites de crédito ou cheque especial. Portanto, o domínio deve validar e impedir a criação de contas do tipo `Cash` com saldo inicial negativo.

### 1.4. Regras para Edição e Exclusão (Alteração)
- Antes de salvar alterações ou deletar uma conta, o sistema deve validar se o `OwnerId` corresponde ao ID do usuário autenticado.
  - Se for idêntico: A operação prossegue.
  - Se pertencer a outro usuário: O sistema bloqueia a operação lançando uma exceção de domínio (mapeada para HTTP `403 Forbidden`).
- **Edição Restrita do Saldo Inicial:** O saldo inicial (`InitialBalance`) pode ser editado através do endpoint de atualização **apenas** enquanto a conta não possuir nenhuma transação (receita, despesa ou transferência) vinculada. Caso possua movimentações, a edição do saldo inicial deve ser bloqueada para preservar o histórico contábil (lançando `DomainException` - HTTP `400 Bad Request`).
- **Fluxo Híbrido de Remoção (Exclusão Física vs. Arquivamento):**
  - **Exclusão Física (Hard Delete):** Se o usuário tentar excluir uma conta que **não possui** nenhuma transação ou transferência vinculada, o sistema deve removê-la fisicamente do banco de dados.
  - **Arquivamento Seguro (Soft Delete):** Se o usuário tentar excluir uma conta que **já possui** movimentações vinculadas, a exclusão física é impedida para evitar dados órfãos. Em vez disso, o sistema deve arquivá-la automaticamente (marcando `IsDeleted = true`).
  - **Integridade Referencial (Cascade Delete):** Para segurança dos dados, as chaves estrangeiras entre tabelas de transação/transferência e contas devem ser configuradas explicitamente no EF Core com exclusão restritiva (`DeleteBehavior.Restrict`). A exclusão em cascata deve ser terminantemente proibida.

---

## 2. Entidade de Conta (`Account`) e Objetos de Valor

Toda a modelagem e nomenclatura de código (classes, propriedades, métodos) do módulo deve ser escrita em **inglês**, enquanto os comentários de código devem ser em **português (pt-BR)**.

Seguindo os princípios de **Object Calisthenics** estabelecidos na Arquitetura Global, os atributos da entidade `Account` são representados por **Objetos de Valor (Value Objects)** ricos e primitivos controlados.

### 2.1. Atributos (Objetos de Valor / Propriedades)
- **Id (`AccountId`):** Objeto de Valor (Strongly Typed ID) que envelopa o identificador único (`Guid`) da conta.
- **Name (`AccountName`):** Objeto de Valor que encapsula, valida e normaliza o nome da conta.
- **Type (`AccountType`):** Enum do domínio que define a natureza da conta:
  - `CheckingAccount` (Conta Corrente)
  - `SavingsAccount` (Conta Poupança)
  - `Investment` (Conta de Investimentos)
  - `Cash` (Dinheiro em Espécie / Carteira Física)
- **InitialBalance (`decimal`):** Saldo de abertura da conta. Pode ser positivo, zero ou negativo (ex: limite de cheque especial ou conta com pendência).
- **OwnerId (`OwnerId`):** Objeto de Valor local do módulo de finanças que envelopa o identificador do usuário proprietário (`Guid`). Obrigatório e não-nulo.
- **CreatedAt (`DateTime`):** Data e hora de criação da conta (UTC).
- **UpdatedAt (`DateTime?`):** Data e hora da última alteração (UTC).
- **IsDeleted (`bool`):** Flag indicando se a conta está arquivada (soft-deleted).

### 2.2. Comportamentos dos Objetos de Valor (Value Objects)
- **`AccountName`:**
  - **Validação:** Não pode ser nulo ou vazio; tamanho mínimo de 3 caracteres e máximo de 100 caracteres.
  - **Comportamento:** Remove espaços sobressalentes (`Trim` e remoção de múltiplos espaços internos) e padroniza a capitalização de forma elegante (ex: "carteira Itaú" -> "Carteira Itaú").

### 2.3. Abstrações do Domínio (`JuliusFinances.Core`)
- **`IAccountRepository` (Interface):**
  - `Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken)`: Busca uma conta por seu ID único.
  - `Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)`: Busca todas as contas pessoais ativas de um usuário.
  - `Task<bool> ExistsByNameAsync(AccountName name, Guid userId, CancellationToken cancellationToken)`: Verifica se já existe uma conta ativa com o mesmo nome para um determinado usuário.
  - `Task<bool> HasLinkedTransactionsAsync(AccountId id, CancellationToken cancellationToken)`: Verifica se a conta possui qualquer transação ou transferência vinculada no banco de dados.
  - `Task AddAsync(Account account, CancellationToken cancellationToken)`: Adiciona uma nova conta.
  - `void Update(Account account)`: Atualiza os dados de uma conta existente.
  - `void Delete(Account account)`: Remove ou arquiva (soft-delete) uma conta.

### 2.4. Comportamentos da Entidade `Account` (Regras de Domínio)
- **Encapsulamento Total:** Todas as propriedades possuem `private set`. As modificações de estado ocorrem estritamente por meio de métodos de negócio.
- **Métodos de Domínio:**
  - `Account(AccountId id, AccountName name, AccountType type, decimal initialBalance, OwnerId ownerId)`: Construtor que garante a criação de uma conta em estado válido. Deve lançar `DomainException` caso o tipo seja `Cash` e o saldo inicial seja menor que zero.
  - `Update(AccountName name, AccountType type, decimal? initialBalance = null, bool hasTransactions = false)`: Atualiza o nome, o tipo e, se aplicável, o saldo inicial da conta (apenas se `hasTransactions` for falso), preenchendo a propriedade `UpdatedAt`.
  - `Archive()`: Marca a propriedade `IsDeleted` como verdadeira, preenchendo o `UpdatedAt`.

---

## 3. Endpoints de Conta (Minimal APIs)

Todas as rotas do módulo de Contas requerem autenticação JWT (`.RequireAuthorization()`). Elas serão organizadas e expostas sob o grupo de rotas `/accounts` em uma classe chamada `AccountEndpoints`.

### 3.1. Listar Contas (`GET /accounts`)
Responsável por retornar as contas ativas do usuário autenticado.

- **Comportamento & Validações:**
  1. Recupera o ID do usuário logado a partir das claims do token JWT.
  2. Executa a busca através do repositório (`GetByUserIdAsync`).
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Conta Corrente Itaú",
      "type": "CheckingAccount",
      "initialBalance": 1000.00
    },
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "name": "Carteira",
      "type": "Cash",
      "initialBalance": 50.00
    }
  ]
  ```

### 3.2. Obter Conta por ID (`GET /accounts/{id}`)
Responsável por retornar os detalhes de uma conta específica.

- **Comportamento & Validações:**
  1. Recupera o ID do usuário logado.
  2. Busca a conta no banco de dados por seu `id`.
  3. Caso a conta não exista, retorna `HTTP 404 Not Found`.
  4. Caso exista, mas pertença a outro usuário (`OwnerId.Value != UserId`), retorna `HTTP 403 Forbidden`.
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "Carteira",
    "type": "Cash",
    "initialBalance": 50.00
  }
  ```

### 3.3. Criar Conta (`POST /accounts`)
Cria uma nova conta associada ao usuário autenticado.

- **Contrato de Entrada (Request Body):**
  ```json
  {
    "name": "Poupança Caixa",
    "type": "SavingsAccount",
    "initialBalance": 500.00
  }
  ```
- **Comportamento & Validações:**
  1. Valida o valor de `type` contra as opções permitidas do Enum.
  2. Instancia o Value Object `AccountName` (disparando `DomainException` em caso de falha de validação - HTTP `400 Bad Request`).
  3. Verifica a duplicidade de nome chamando `ExistsByNameAsync(name, userId)`. Caso já exista uma conta idêntica ativa para o usuário, lança `AccountNameAlreadyExistsException` (HTTP `409 Conflict`).
  4. Se o tipo de conta for `Cash`, valida se o `initialBalance` é menor que zero. Se for, retorna HTTP `400 Bad Request`.
  5. Salva a nova entidade vinculando o `OwnerId` ao ID do usuário autenticado.
- **Contrato de Saída (Response HTTP 201 Created):**
  ```json
  {
    "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "Poupança Caixa",
    "type": "SavingsAccount",
    "initialBalance": 500.00
  }
  ```

### 3.4. Atualizar Conta (`PUT /accounts/{id}`)
Permite ao usuário editar o nome, tipo de sua conta pessoal ou o saldo inicial (se a conta ainda não contiver transações).

- **Contrato de Entrada (Request Body):**
  ```json
  {
    "name": "Poupança Caixa Atualizada",
    "type": "SavingsAccount",
    "initialBalance": 600.00
  }
  ```
- **Comportamento & Validações:**
  1. Busca a conta pelo ID fornecido. Se não existir, retorna `HTTP 404 Not Found`.
  2. Verifica as permissões de edição: se `OwnerId.Value != UserId`, retorna `HTTP 403 Forbidden`.
  3. Valida se o novo nome é duplicado para o usuário (ignorando o próprio registro atual). Se for duplicado, retorna `HTTP 409 Conflict`.
  4. Verifica se o corpo da requisição tenta alterar o `initialBalance`. Se houver alteração de saldo inicial:
     - Consulta o repositório (`HasLinkedTransactionsAsync`) para checar se a conta possui transações vinculadas.
     - Se houver transações vinculadas, lança `DomainException` informando que o saldo inicial não pode ser editado (HTTP `400 Bad Request`).
  5. Se o tipo de conta for atualizado para `Cash`, valida se o saldo inicial resultante é menor que zero. Se for, retorna HTTP `400 Bad Request`.
  6. Executa a atualização por meio do método de domínio `Update(newName, newType, newInitialBalance, hasTransactions)` e persiste no banco de dados.
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  {
    "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "Poupança Caixa Atualizada",
    "type": "SavingsAccount",
    "initialBalance": 600.00
  }
  ```

### 3.5. Excluir/Arquivar Conta (`DELETE /accounts/{id}`)
Gerencia a remoção inteligente de uma conta pessoal do usuário autenticado através de exclusão física ou arquivamento automático.

- **Comportamento & Validações:**
  1. Busca a conta pelo ID fornecido. Se não encontrar, retorna `HTTP 404 Not Found`.
  2. Verifica permissões: se pertencer a outro usuário, impede a operação e retorna `HTTP 403 Forbidden`.
  3. Executa o fluxo de exclusão/arquivamento de forma segura dentro de uma transação isolada para prevenir condições de corrida:
     - Consulta `HasLinkedTransactionsAsync(id)` no banco de dados.
     - **Caso NÃO possua transações vinculadas:** O repositório executa a exclusão física (`DELETE`) do registro.
     - **Caso POSSUA transações vinculadas:** O sistema chama o método de domínio `Archive()`, aplicando o soft delete (`IsDeleted = true`).
  4. Persiste as mudanças e faz o commit da transação.
- **Contrato de Saída (Response HTTP 204 No Content):**
  *(Corpo vazio)*

---

## 4. Criação Automática de Conta Padrão (Onboarding)

Para otimizar o fluxo de onboarding e usabilidade imediata (User Experience), o processo de criação de uma nova conta de usuário (`User`) aciona a criação de uma conta financeira padrão de forma assíncrona e totalmente desacoplada.

### 4.1. Estratégia de Onboarding via Eventos de Domínio
- **Desacoplamento por Eventos:** Ao finalizar o cadastro com sucesso de um novo usuário, o módulo de autenticação (`Auth`) publica o evento de domínio **`UserRegisteredEvent`** (envelopando o `UserId`).
- **Assinatura e Manipulação:** O módulo de configuração de finanças (`FinancesSetup`) registra o assinante **`UserRegisteredEventHandler`** que escuta esse evento.
- **Criação da Conta Padrão:** O handler consome o evento e cria de forma autônoma uma conta padrão para o usuário com os seguintes valores pré-definidos:
  - **Nome:** `"Carteira"`
  - **Tipo:** `Cash`
  - **Saldo Inicial:** `0.00`
- **Benefício:** Evita o acoplamento temporal e síncrono direto entre os módulos de `Auth` e `FinancesSetup`. Caso ocorra alguma falha pontual na criação da carteira financeira padrão, o registro de usuário não é bloqueado, e o fluxo pode ser reprocessado de maneira resiliente.

---

## 5. Sugestões de Melhorias e Boas Práticas (Arquitetura & UX)

A fim de enriquecer a implementação do módulo de contas no JuliusFinances, as seguintes práticas de engenharia são recomendadas para a camada de desenvolvimento:

1. **Uso de Filtros Globais de Consulta no EF Core:**
   A estratégia de **Soft Delete** deve ser facilitada através do mapeamento de um query filter global no EF Core para a entidade `Account` (`builder.HasQueryFilter(a => !a.IsDeleted)`). Isso garante que contas arquivadas fiquem invisíveis em seletores ativos e listagens por padrão, simplificando as consultas da aplicação.
2. **Impacto em Relatórios e Dashboards Globais:**
   - As contas arquivadas (`IsDeleted == true`) devem continuar sendo elegíveis para cálculos retroativos de gráficos de evolução patrimonial e históricos contábeis.
   - Para carregar dados de contas arquivadas especificamente para fins de relatórios e balanços passados, as consultas do EF Core devem desativar temporariamente o filtro global utilizando o método `.IgnoreQueryFilters()`.
3. **Ícones e Cores de Identificação Visual:**
   Para enriquecer a interface do usuário e facilitar a identificação visual no dashboard do frontend, sugere-se adicionar à entidade `Account`:
   - `IconKey`: Ex: `"wallet"`, `"bank"`, `"credit-card"`, `"trending-up"`.
   - `HexColor`: Cor personalizada para exibir nas telas do app (ex: `"#00E676"` para carteira física, `"#29B6F6"` para banco digital).
4. **Gerenciamento de Saldo Dinâmico vs Saldo Inicial:**
   - A propriedade `InitialBalance` representa o ponto de partida histórico da conta.
   - O saldo atual consolidado da conta deverá ser obtido somando o `InitialBalance` com o somatório algebraico de todas as transações (créditos - débitos) e transferências vinculadas a essa conta.
