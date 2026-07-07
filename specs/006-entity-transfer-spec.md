# Módulo de Configuração de Finanças - Entidade Transferência

Este módulo (Finances Setup) é responsável por gerenciar as transferências financeiras (Transfer) no JuliusFinances. As transferências representam a movimentação de recursos entre duas contas distintas pertencentes ao mesmo usuário (débito na conta de origem e crédito na conta de destino). Diferente das transações convencionais, uma transferência não altera o patrimônio líquido global do usuário, alterando apenas a distribuição dos saldos entre suas contas.

*Nota: Toda a infraestrutura técnica (tratamento global de exceções, padrões de persistência com Value Converters e Owned Entities, gerenciamento seguro de segredos e organização de rotas de Minimal APIs) deve seguir estritamente o definido na [Especificação de Arquitetura Global](000-global-architecture-spec.md).*

---

## 1. Regras de Negócio e Comportamento

### 1.1. Visibilidade, Isolamento e Multi-inquilinato (Multi-tenant)
* **Isolamento Total:** Todas as transferências no sistema são privadas, individuais e pertencem obrigatoriamente a um usuário autenticado (`OwnerId` obrigatório e não-nulo)[cite: 5, 6]. O Usuário A nunca poderá visualizar, utilizar, editar ou excluir as transferências do Usuário B[cite: 6]. Qualquer tentativa de acesso cruzado indevido disparará uma exceção de domínio mapeada para HTTP `403 Forbidden`[cite: 6].

### 1.2. Consistência e Vínculos de Domínio (Validações de Integridade)
Ao registrar ou alterar uma transferência, o sistema deve realizar validações de integridade cruzadas utilizando os componentes da arquitetura limpa[cite: 1, 6]. As responsabilidades de validação são distribuídas da seguinte forma:

#### A. Camada de Aplicação (Endpoints / Casos de Uso)
Antes de invocar o construtor ou o método de atualização da transferência, o caso de uso correspondente deve garantir:
* **Existência e Propriedade da Conta de Origem (`OriginAccountId`):** A conta de débito deve existir, pertencer ao usuário autenticado e não estar arquivada (`IsDeleted == false`)[cite: 5, 6].
* **Existência e Propriedade da Conta de Destino (`DestinationAccountId`):** A conta de crédito deve existir, pertencer ao usuário autenticado e não estar arquivada (`IsDeleted == false`)[cite: 5, 6].
* **Diferenciação de Contas:** A conta de origem e a conta de destino devem ser obrigatoriamente diferentes. Caso sejam iguais, o caso de uso deve rejeitar a operação retornando HTTP `400 Bad Request`.
* **Vínculo Automatizado de Categoria:** O caso de uso deve injetar de forma implícita e automática o ID da categoria global canônica de **Transferência** (`de250014-c812-4c22-9014-99859f123456`)[cite: 4] para fins de indexação e relatórios unificados de movimentação[cite: 6].

### 1.3. Regras para Criação (Escrita)
* **Validação do Valor (`Money`):** O valor monetário da transferência deve ser encapsulado e validado pelo domínio rico (`Money`)[cite: 6]. O montante (`Amount`) deve ser estritamente maior que zero e menor ou igual a `99,999,999,999.99`[cite: 6].
* **Restrição de Moeda (`Currency`):** O único código de moeda aceito pelo domínio rico é `"BRL"`[cite: 6]. Qualquer valor diferente deve disparar uma `DomainException` (HTTP `400 Bad Request`)[cite: 1, 6].
* **Data da Transferência (`TransferDate`):** Representa o momento de competência do evento financeiro, permitindo datas retroativas ou futuras[cite: 6]. Qualquer data fornecida pelo cliente deve ser explicitamente tratada e gravada como UTC usando `DateTime.SpecifyKind(transferDate, DateTimeKind.Utc)` para evitar desvios locais de fuso horário ao persistir no banco de dados.
  * **Limites de Segurança de Data:** Para evitar problemas de estouro de data (overflow/underflow) e inconsistências nas conversões do banco de dados, o ano da `TransferDate` deve estar obrigatoriamente compreendido entre o ano **2000** e o ano **2100**. Datas fora desse intervalo devem disparar uma exceção de domínio (HTTP `400 Bad Request`).

### 1.4. Regras para Edição e Exclusão (Alteração)
* **Validação de Propriedade:** Antes de salvar alterações ou arquivar uma transferência, o sistema deve validar se o `OwnerId` da transferência corresponde ao ID do usuário autenticado[cite: 6]. Se pertencer a outro usuário, a operação é bloqueada com HTTP `403 Forbidden`[cite: 6].
* **Arquivamento Lógico (Soft-Delete):** Para a manutenção de relatórios históricos e consistência contábil, transferências nunca devem ser removidas fisicamente do banco de dados[cite: 6]. O sistema adota o Soft-Delete mudando a propriedade para `IsDeleted == true`[cite: 6].
* **Impacto no Histórico de Contas e Reversibilidade:** A criação, alteração de valor, modificação das contas envolvidas ou arquivamento de uma transferência impacta diretamente a mutabilidade do saldo inicial das contas afetadas[cite: 6].
  * Conforme as regras do módulo de contas, a existência de qualquer transferência associada ativa (`IsDeleted == false`) bloqueia definitivamente a edição do saldo inicial (`InitialBalance`) tanto da conta de origem quanto da conta de destino.
  * Esse desbloqueio de saldo ocorre de forma inteiramente dinâmica e implícita: ao arquivar ou alterar as contas de uma transferência ativa, as consultas de verificação (`HasLinkedTransactionsAsync` no repositório) deixarão de encontrar movimentações vinculadas para as contas antigas. Assim, a edição do seu saldo inicial (`InitialBalance`) passa a ser liberada automaticamente e de forma instantânea nas próximas requisições de atualização da conta, sem necessidade de processos assíncronos ou alteração de estado persistido nas contas.

---

## 2. Entidade de Transferência (`Transfer`) e Objetos de Valor

Toda a modelagem e nomenclatura de código (classes, propriedades, métodos) do módulo deve ser escrita em **inglês**, enquanto os comentários de código devem ser em **português (pt-BR)**[cite: 2, 4, 5, 6].

Seguindo os princípios de **Object Calisthenics** estabelecidos na Arquitetura Global, os atributos da entidade `Transfer` são representados por **Objetos de Valor (Value Objects)** ricos ou propriedades controladas[cite: 1, 6].

### 2.1. Atributos (Objetos de Valor / Propriedades)
* **Id (`TransferId`):** Objeto de Valor (Strongly Typed ID) que envelopa o identificador único (`Guid`) da transferência[cite: 2, 4, 5, 6].
* **Description (`TransferDescription`):** Objeto de Valor que encapsula e valida a descrição textual opcional da movimentação.
* **Money (`Money`):** Objeto de valor composto que encapsula o comportamento financeiro[cite: 1, 6]. Mapeado utilizando **Owned Entity Types** (`OwnsOne`) no EF Core[cite: 1, 6].
  * `Amount` (`decimal`): O valor numérico estritamente positivo da movimentação[cite: 6].
  * `Currency` (`string`): A moeda da transação (padrão estável: `"BRL"`)[cite: 6].
* **OriginAccountId (`AccountId`):** Identificador fortemente tipado da conta de onde os recursos serão debitados[cite: 5, 6].
* **DestinationAccountId (`AccountId`):** Identificador fortemente tipado da conta onde os recursos serão creditados[cite: 5, 6].
* **CategoryId (`CategoryId`):** Identificador fortemente tipado apontando fixamente para a categoria global de transferência[cite: 4, 6].
* **OwnerId (`OwnerId`):** Objeto de Valor local do módulo de finanças que envelopa o identificador do usuário proprietário (`Guid`)[cite: 5, 6].
* **TransferDate (`DateTime`):** Data e hora de ocorrência da transferência (UTC)[cite: 6].
* **CreatedAt (`DateTime`):** Data e hora de criação do registro (UTC)[cite: 2, 4, 5, 6].
* **UpdatedAt (`DateTime?`):** Data e hora da última alteração (UTC)[cite: 2, 4, 5, 6].
* **IsDeleted (`bool`):** Indicador lógico de arquivamento (Soft-Delete) do registro[cite: 6].

### 2.2. Comportamentos dos Objetos de Valor (Value Objects)
* **`TransferDescription`:**
  * **Validação:** Pode ser nulo ou vazio; se preenchido, deve conter tamanho mínimo de 3 caracteres e máximo de 250 caracteres.
  * **Comportamento:** Remove espaços sobressalentes nas extremidades (`Trim`)[cite: 6]. Se vazio, assume uma descrição padrão (ex: "Transferência entre Contas").

### 2.3. Abstrações do Domínio (`JuliusFinances.Core`)
* **`ITransferRepository` (Interface):**
  * `Task<Transfer?> GetByIdAsync(TransferId id, CancellationToken cancellationToken)`: Busca uma transferência por seu ID único.
  * `Task<IEnumerable<Transfer>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)`: Busca a listagem de transferências ativas do usuário de forma paginada.
  * `Task AddAsync(Transfer transfer, CancellationToken cancellationToken)`: Adiciona uma nova transferência.
  * `void Update(Transfer transfer)`: Atualiza os dados de uma transferência existente.
  * `void Delete(Transfer transfer)`: Arquiva logicamente uma transferência (Soft-Delete)[cite: 6].

### 2.4. Comportamentos da Entidade `Transfer` (Regras de Domínio)
* **Encapsulamento Total:** Todas as propriedades possuem `private set`[cite: 2, 4, 5, 6]. As modificações de estado ocorrem estritamente por meio de métodos de negócio[cite: 6].
* **Métodos de Domínio:**
  * `Transfer(TransferId id, TransferDescription description, Money money, AccountId originAccountId, AccountId destinationAccountId, CategoryId categoryId, OwnerId ownerId, DateTime transferDate)`: Construtor que valida e cria a entidade em estado íntegro, definindo `CreatedAt` e inicializando `IsDeleted` como `false`[cite: 6]. Garante que `transferDate` seja explicitamente tratada em UTC via `DateTime.SpecifyKind(transferDate, DateTimeKind.Utc)`[cite: 6]. Lança `DomainException` se as duas contas forem idênticas[cite: 1].
  * `Update(TransferDescription description, Money money, AccountId originAccountId, AccountId destinationAccountId, DateTime transferDate)`: Atualiza os dados permitidos da transferência, garante `transferDate` em UTC via `DateTime.SpecifyKind(transferDate, DateTimeKind.Utc)` e atualiza a propriedade `UpdatedAt`[cite: 6]. Revalida a diferenciação entre as contas de origem e destino.
  * `Archive()`: Aplica o arquivamento lógico marcando `IsDeleted = true` e atualizando `UpdatedAt` com a data atual em UTC[cite: 6].

---

## 3. Endpoints de Transferência (Minimal APIs)

Todas as rotas da entidade de Transferências requerem autenticação JWT (`.RequireAuthorization()`)[cite: 2, 4, 5, 6]. Elas serão organizadas e expostas sob o grupo de rotas `/transfers` em uma classe chamada `TransferEndpoints`[cite: 1, 6].

### 3.1. Listar Transferências (`GET /transfers`)
Retorna as movimentações de transferência ativas (`IsDeleted == false`) do usuário autenticado de forma paginada[cite: 6].

* **Parâmetros de Query:**
  * `page` (opcional, padrão: 1): Deve ser maior ou igual a 1[cite: 6].
  * `pageSize` (opcional, padrão: 20): Deve estar restrito entre 1 e 100[cite: 6].
* **Comportamento:** O repositório deve retornar as transferências pertencentes ao usuário ordenadas por `TransferDate DESC`, utilizando `CreatedAt DESC` como critério de desempate[cite: 6].
* **Contrato de Saída (Response HTTP 200 OK):**
```json
[
  {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66af20",
    "description": "Aplicação na Poupança",
    "money": {
      "amount": 500.00,
      "currency": "BRL"
    },
    "originAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "destinationAccountId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "categoryId": "de250014-c812-4c22-9014-99859f123456",
    "transferDate": "2026-07-06T19:15:00Z"
  }
]

```

### 3.2. Obter Transferência por ID (`GET /transfers/{id}`)

Retorna os detalhes de uma transferência específica.

* **Comportamento & Validações:**
1. Busca a transferência no banco de dados. Caso não exista ou esteja com `IsDeleted == true`, retorna `HTTP 404 Not Found`.


2. Valida se a transferência pertence ao usuário logado (`OwnerId.Value != UserId`). Caso não pertença, retorna `HTTP 403 Forbidden`.




* **Contrato de Saída (Response HTTP 200 OK):**

```json
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66af20",
  "description": "Aplicação na Poupança",
  "money": {
    "amount": 500.00,
    "currency": "BRL"
  },
  "originAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "destinationAccountId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "categoryId": "de250014-c812-4c22-9014-99859f123456",
  "transferDate": "2026-07-06T19:15:00Z"
}

```

### 3.3. Criar Transferência (`POST /transfers`)

Registra uma nova transferência entre duas contas do usuário autenticado.

* **Contrato de Entrada (Request Body):**

```json
{
  "description": "Resgate de Investimento",
  "amount": 1200.00,
  "currency": "BRL",
  "originAccountId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "destinationAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "transferDate": "2026-07-06T20:00:00Z"
}

```

* **Comportamento & Validações:**
1. **Validação Primária:** Rejeita se `originAccountId == destinationAccountId` (HTTP `400 Bad Request`). A validação de moeda (`currency == "BRL"`) e valor positivo é delegada e executada de forma nativa pelo Objeto de Valor `Money` (HTTP `400 Bad Request` via `DomainException`).


2. **Verificação de Origem e Destino:** Ambas as contas informadas devem existir, pertencer ao usuário logado e não estar arquivadas. Violações de existência retornam `HTTP 404 Not Found`, violações de propriedade retornam `HTTP 403 Forbidden` e contas arquivadas retornam `HTTP 400 Bad Request`.


3. **Injeção de Categoria:** Atribui automaticamente o ID estático `de250014-c812-4c22-9014-99859f123456` correspondente à categoria global de transferência.


4. **Timezone & Persistência:** Converte `transferDate` para UTC e salva a nova entidade `Transfer` vinculando o `OwnerId`.




* **Contrato de Saída (Response HTTP 201 Created):**

```json
{
  "id": "afa85f64-5717-4562-b3fc-2c963f66af21",
  "description": "Resgate de Investimento",
  "money": {
    "amount": 1200.00,
    "currency": "BRL"
  },
  "originAccountId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "destinationAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250014-c812-4c22-9014-99859f123456",
  "transferDate": "2026-07-06T20:00:00Z"
}

```

### 3.4. Atualizar Transferência (`PUT /transfers/{id}`)

Permite ao usuário editar a descrição, valor, data ou as contas de uma transferência de sua propriedade.

* **Contrato de Entrada (Request Body):**

```json
{
  "description": "Resgate de Investimento (Ajustado)",
  "amount": 1250.00,
  "currency": "BRL",
  "originAccountId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "destinationAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "transferDate": "2026-07-06T20:00:00Z"
}

```

* **Comportamento & Validações:**
1. **Busca e Permissão:** Se a transferência não existir ou estiver arquivada, retorna `HTTP 404 Not Found`. Se `OwnerId.Value != UserId`, retorna `HTTP 403 Forbidden`.


2. **Revalidação das Contas:** Valida as novas contas enviadas (ou as mantidas) garantindo que existam, pertençam ao usuário, sejam diferentes entre si e não estejam arquivadas.


3. **Atualização Contábil:** Persiste a alteração no repositório. O desbloqueio do saldo inicial de qualquer conta que teve seu vínculo removido ocorre de forma dinâmica e síncrona através da consulta de verificação (`HasLinkedTransactionsAsync`) nas requisições subsequentes de atualização de conta.





### 3.5. Excluir/Arquivar Transferência (`DELETE /transfers/{id}`)

Arquiva logicamente o registro de transferência do usuário autenticado.

* **Comportamento:**
1. Se a transferência não for encontrada ou já estiver com `IsDeleted == true`, retorna `HTTP 404 Not Found`. Se pertencer a outro usuário, retorna `HTTP 403 Forbidden`.


2. Executa o método `Archive()` na entidade e persiste no repositório mudando o status para soft-delete.


3. **Desbloqueio de Saldo Inicial:** Após a persistência do arquivamento (soft-delete), as contas envolvidas (origem e destino) passam a ter a edição de seu saldo inicial (`InitialBalance`) desbloqueada de forma implícita e dinâmica. Isso ocorre porque a query do repositório (`HasLinkedTransactionsAsync`) não localizará mais esta transferência ativa, liberando a edição de forma nativa e sem processos adicionais em background.




* **Contrato de Saída (Response HTTP 204 No Content):**
*(Corpo vazio)*


---

## 4. Sugestões de Melhorias e Boas Práticas (Arquitetura & UX)

1. **Indexação de Performance no PostgreSQL:**
Para otimizar o tempo de resposta nas consultas históricas cruzadas de movimentações patrimoniais, configure índices compostos na Fluent API do Entity Framework Core na entidade `Transfer`:



```csharp
builder.HasIndex(t => new { t.OwnerId, t.TransferDate });
builder.HasIndex(t => new { t.OwnerId, t.OriginAccountId });
builder.HasIndex(t => new { t.OwnerId, t.DestinationAccountId });

```

2. **Cálculo do Saldo Dinâmico Amparando Transferências:**
A regra de consolidação de saldo descrita na especificação de contas e transações deve ser atualizada para refletir algebricamente o impacto das transferências ativas (`IsDeleted == false`):



* **Saldo Atual da Conta** = `InitialBalance` + Sum(`Income` Transactions) - Sum(`Expense` Transactions) + Sum(Transferências como `DestinationAccountId`) - Sum(Transferências como `OriginAccountId`).



3. **Atualização dos Repositórios Existentes (`Account`):**
A implementação do método `HasLinkedTransactionsAsync` em `AccountRepository.cs` deve obrigatoriamente expandir sua varredura para checar a tabela de transferências. Uma conta passa a ser considerada vinculada se houver qualquer transação ativa OU se ela figurar como conta de origem ou destino em uma transferência ativa:



```csharp
public async Task<bool> HasLinkedTransactionsAsync(AccountId id, CancellationToken cancellationToken)
{
    var hasTransactions = await _dbContext.Transactions.AnyAsync(t => t.AccountId == id && !t.IsDeleted, cancellationToken);
    if (hasTransactions) return true;

    return await _dbContext.Transfers.AnyAsync(t => (t.OriginAccountId == id || t.DestinationAccountId == id) && !t.IsDeleted, cancellationToken);
}
```
