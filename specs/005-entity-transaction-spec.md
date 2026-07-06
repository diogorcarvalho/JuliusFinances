# Módulo de Configuração de Finanças - Entidade Transação

Este módulo (Finances Setup) é responsável por gerenciar as transações financeiras (Transaction) no JuliusFinances. As transações são o núcleo operacional do sistema, representando as movimentações diárias de entradas (receitas) e saídas (despesas) dos usuários. A entidade transação consolida as regras do módulo, vinculando obrigatoriamente uma movimentação a uma conta específica e a uma categoria de classificação.

*Nota: Toda a infraestrutura técnica (tratamento global de exceções, padrões de persistência com Value Converters e Owned Entities, gerenciamento seguro de segredos e organização de rotas de Minimal APIs) deve seguir estritamente o definido na [Especificação de Arquitetura Global](000-global-architecture-spec.md).*
---

## 1. Regras de Negócio e Comportamento

### 1.1. Visibilidade, Isolamento e Multi-inquilinato (Multi-tenant)

* **Isolamento Total:** Todas as transações no sistema são privadas, individuais e pertencem obrigatoriamente a um usuário autenticado (`OwnerId` obrigatório e não-nulo). O Usuário A nunca poderá visualizar, utilizar, editar ou excluir as transações do Usuário B. Qualquer tentativa de acesso cruzado indevido disparará uma exceção de domínio mapeada para HTTP `403 Forbidden`[cite: 1, 4, 5].



### 1.2. Consistência e Vínculos de Domínio (Validações de Integridade)

Ao registrar ou alterar uma transação, o sistema deve realizar validações de integridade entre as entidades do próprio módulo `Finances Setup`. Devido ao desacoplamento da arquitetura limpa (onde a entidade de domínio rico `Transaction` manipula apenas Strongly Typed IDs e não se conecta a repositórios), as responsabilidades de validação são distribuídas da seguinte forma:

#### A. Camada de Aplicação (Endpoints / Casos de Uso)
Antes de invocar o construtor ou método de atualização da transação, o caso de uso deve garantir:
* **Existência e Propriedade da Conta (`AccountId`):** A conta informada para a transação deve existir e pertencer obrigatoriamente ao usuário autenticado (`OwnerId`). Contas arquivadas (`IsDeleted == true`) devem ser rejeitadas.
* **Existência e Propriedade da Categoria (`CategoryId`):** A categoria informada deve existir e ser acessível ao usuário (seja uma categoria pessoal ou uma categoria global). Categorias arquivadas (`IsDeleted == true`) devem ser rejeitadas.
* **Recuperação de Informações do Domínio:** O caso de uso deve ler o `FlowType` da categoria associada para repassar ou validar as regras de compatibilidade de fluxo descritas abaixo.

#### B. Camada de Domínio (`Transaction` / Entidade)
* **Compatibilidade de Fluxo (`FlowType`):** O tipo da transação (`Income` ou `Expense`) deve ser estritamente compatível com o tipo de fluxo permitido pela categoria.
  * Se a transação for do tipo `Expense` (Despesa), a categoria associada deve possuir o `FlowType` configurado como `Expense` ou `Both`.
  * Se a transação for do tipo `Income` (Receita), a categoria associada deve possuir o `FlowType` configurado como `Income` ou `Both`.
  * Caso haja incompatibilidade de fluxo, a operação deve ser bloqueada lançando uma `DomainException` (mapeada para HTTP `400 Bad Request`)[cite: 1].



### 1.3. Regras para Criação (Escrita)

* **Validação do Valor (`Money`):** O valor monetário da transação deve ser encapsulado e validado pelo domínio rico (`Money`). O valor (`Amount`) deve ser estritamente maior que zero e menor ou igual a `99,999,999,999.99` para evitar estouros numéricos no banco de dados. 
* **Restrição de Moeda (`Currency`):** Como o sistema não possui taxas de câmbio ativas e as contas são armazenadas sem moedas explícitas, o único valor aceito para a propriedade `Currency` é `"BRL"`. Qualquer valor diferente deve ser rejeitado pelo domínio rico lançando uma exceção (`DomainException` mapeada para `400 Bad Request`).
* **Data da Transação (`TransactionDate`):** Representa o momento real de competência do evento financeiro. O sistema permite o registro de transações com datas retroativas ou futuras. No entanto, para fins de consistência temporal e relatórios, qualquer data fornecida deve ser convertida explicitamente e registrada como UTC no banco de dados.

### 1.4. Regras para Edição e Exclusão (Alteração)

* **Validação de Propriedade:** Antes de salvar alterações ou arquivar uma transação, o sistema deve validar se o `OwnerId` da transação corresponde ao ID do usuário autenticado.
  * Se for idêntico: A operação prossegue.
  * Se pertencer a outro usuário: O sistema bloqueia a operação disparando uma exceção de acesso proibido (HTTP `403 Forbidden`).
* **Arquivamento Lógico (Soft-Delete):** Para fins de consistência contábil, auditoria e manutenção de relatórios históricos, transações **nunca** devem ser removidas fisicamente do banco de dados. Em vez disso, o sistema adota o padrão de Soft-Delete (arquivamento lógico com propriedade `IsDeleted == true`).
* **Impacto no Histórico de Contas e Reversibilidade:** A criação, alteração do valor, mudança do vínculo da conta ou arquivamento de uma transação altera o saldo e o histórico financeiro da conta vinculada.
  * Conforme as regras da entidade `Account`, a existência de qualquer transação associada ativa e não arquivada (`IsDeleted == false`) bloqueia definitivamente a edição do saldo inicial (`InitialBalance`) daquela conta.
  * Se todas as transações de uma conta forem arquivadas, ou se uma transação ativa tiver seu vínculo de conta alterado para outra conta (via `PUT`), a conta de origem, caso não possua mais nenhuma transação ativa vinculada, **deve ter a edição do seu saldo inicial desbloqueada automaticamente**.

---

## 2. Entidade de Transação (`Transaction`) e Objetos de Valor

Toda a modelagem e nomenclatura de código (classes, propriedades, métodos) do módulo deve ser escrita em **inglês**, enquanto os comentários de código devem ser em **português (pt-BR)**[cite: 2, 4, 5].

Seguindo os princípios de **Object Calisthenics** estabelecidos na Arquitetura Global, os atributos da entidade `Transaction` são representados por **Objetos de Valor (Value Objects)** ricos ou propriedades controladas[cite: 1].

### 2.1. Atributos (Objetos de Valor / Propriedades)

* **Id (`TransactionId`):** Objeto de Valor (Strongly Typed ID) que envelopa o identificador único (`Guid`) da transação[cite: 2, 4, 5].
* **Description (`TransactionDescription`):** Objeto de Valor que encapsula e valida a descrição textual da movimentação.
* **Type (`TransactionType`):** Enum do domínio que especifica a natureza do fluxo (`Income` ou `Expense`).
* **Money (`Money`):** Objeto de valor composto que encapsula o comportamento financeiro[cite: 1]. Conforme definido na arquitetura global, deve ser mapeado utilizando **Owned Entity Types** (`OwnsOne`) no EF Core, garantindo o armazenamento correto em colunas na tabela de transações[cite: 1].
  * `Amount` (`decimal`): O valor numérico estritamente positivo da movimentação[cite: 1].
  * `Currency` (`string`): A moeda da transação (padrão estável: `"BRL"`).
* **AccountId (`AccountId`):** Identificador fortemente tipado de amarração com a entidade `Account`[cite: 5].
* **CategoryId (`CategoryId`):** Identificador fortemente tipado de amarração com a entidade `Category`.
* **OwnerId (`OwnerId`):** Objeto de Valor local do módulo de finanças que envelopa o identificador do usuário proprietário (`Guid`). Obrigatório e não-nulo[cite: 5]. Isso evita acoplamento direto com as classes do domínio `Auth`.
* **TransactionDate (`DateTime`):** Data e hora de ocorrência da transação (UTC).
* **CreatedAt (`DateTime`):** Data e hora de criação do registro (UTC)[cite: 2, 4, 5].
* **UpdatedAt (`DateTime?`):** Data e hora da última alteração (UTC)[cite: 2, 4, 5].
* **IsDeleted (`bool`):** Indicador lógico de arquivamento (Soft-Delete) do registro.

### 2.2. Comportamentos dos Objetos de Valor (Value Objects)

* **`TransactionDescription`:**
  * **Validação:** Não pode ser nulo ou vazio; tamanho mínimo de 3 caracteres e máximo de 250 caracteres.
  * **Comportamento:** Remove espaços sobressalentes nas extremidades (`Trim`).

* **`Money`:**
  * **Validação:** O `Amount` deve ser estritamente maior que zero e menor ou igual a `99,999,999,999.99`[cite: 1]. O código da moeda (`Currency`) deve ser obrigatoriamente `"BRL"`. Qualquer outro código deve disparar uma `DomainException`.

### 2.3. Abstrações do Domínio (`JuliusFinances.Core`)

* **`ITransactionRepository` (Interface):**
  * `Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken)`: Busca uma transação por seu ID único.
  * `Task<IEnumerable<Transaction>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)`: Busca a listagem de transações ativas e não arquivadas do usuário de forma paginada.
  * `Task AddAsync(Transaction transaction, CancellationToken cancellationToken)`: Adiciona uma nova transação.
  * `void Update(Transaction transaction)`: Atualiza os dados de uma transação existente.
  * `void Delete(Transaction transaction)`: Arquiva logicamente uma transação (Soft-Delete), marcando `IsDeleted = true` e atualizando o registro.

### 2.4. Comportamentos da Entidade `Transaction` (Regras de Domínio)

* **Encapsulamento Total:** Todas as propriedades possuem `private set`[cite: 2, 4, 5]. As modificações de estado ocorrem estritamente por meio de métodos de negócio.

* **Métodos de Domínio:**
  * `Transaction(TransactionId id, TransactionDescription description, TransactionType type, Money money, AccountId accountId, CategoryId categoryId, OwnerId ownerId, DateTime transactionDate)`: Construtor que garante a criação de uma transação em estado válido, preenche o `CreatedAt` e inicializa `IsDeleted` como `false`. Converte e garante que `transactionDate` seja persistida em UTC.
  * `Update(TransactionDescription description, Money money, AccountId accountId, CategoryId categoryId, DateTime transactionDate)`: Atualiza os dados permitidos da transação, garante que `transactionDate` seja persistida em UTC e preenche a propriedade `UpdatedAt`.
  * `Archive()`: Arquiva logicamente a transação marcando `IsDeleted = true` e atualizando `UpdatedAt` com a data atual em UTC.





---

## 3. Endpoints de Transação (Minimal APIs)

Todas as rotas da entidade de Transações requerem autenticação JWT (`.RequireAuthorization()`)[cite: 2, 4, 5]. Elas serão organizadas e expostas sob o grupo de rotas `/transactions` em uma classe chamada `TransactionEndpoints` dentro da estrutura de entrega do módulo de finanças[cite: 1].

### 3.1. Listar Transações Pagina e Filtrada (`GET /transactions`)

Responsável por retornar as movimentações financeiras ativas (`IsDeleted == false`) do usuário autenticado de forma paginada, com filtros opcionais e ordenação consistente.

* **Parâmetros de Query (Filtros e Paginação):**
  * `page` (opcional, padrão: 1): Deve ser maior ou igual a 1. Se menor, assume o padrão 1.
  * `pageSize` (opcional, padrão: 20): Deve estar entre 1 e 100. Se fora desses limites, assume o padrão 20 para evitar exaustão de recursos.
  * `accountId` (opcional): Filtra as transações de uma conta específica do usuário[cite: 5].
  * `categoryId` (opcional): Filtra as transações de uma categoria específica do usuário ou global.

* **Comportamento & Validações:**
  1. O sistema deve recuperar apenas as transações que pertencem ao usuário autenticado (`OwnerId == UserId`) e que não foram arquivadas (`IsDeleted == false`).
  2. As transações retornadas devem ser obrigatoriamente ordenadas por data de ocorrência de forma decrescente (`TransactionDate DESC`), utilizando a data de criação (`CreatedAt DESC`) como critério de desempate para garantir a consistência de visualização contábil.

* **Contrato de Saída (Response HTTP 200 OK):**
```json
[
  {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66af10",
    "description": "Supermercado Compre Bem",
    "type": "Expense",
    "money": {
      "amount": 250.50,
      "currency": "BRL"
    },
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryId": "de250001-c812-4c22-9014-99859f123456",
    "transactionDate": "2026-07-06T14:30:00Z"
  }
]

```



### 3.2. Obter Transação por ID (`GET /transactions/{id}`)

Responsável por retornar os detalhes de uma transação específica.

* **Comportamento & Validações:**
  1. Recupera o ID do usuário logado.
  2. Busca a transação no banco de dados por seu `id`.
  3. Caso a transação não exista, ou esteja arquivada (`IsDeleted == true`), retorna `HTTP 404 Not Found`.
  4. Caso exista (mesmo se arquivada, para fins de barreira de segurança), mas pertença a outro usuário (`OwnerId.Value != UserId`), retorna `HTTP 403 Forbidden`.




* **Contrato de Saída (Response HTTP 200 OK):**
```json
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66af10",
  "description": "Supermercado Compre Bem",
  "type": "Expense",
  "money": {
    "amount": 250.50,
    "currency": "BRL"
  },
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250001-c812-4c22-9014-99859f123456",
  "transactionDate": "2026-07-06T14:30:00Z"
}

```



### 3.3. Criar Transação (`POST /transactions`)

Cria uma nova transação associada ao usuário autenticado, validando as dependências do módulo[cite: 2, 4, 5].

* **Contrato de Entrada (Request Body):**
```json
{
  "description": "Freelance Desenvolvimento Dashboard",
  "type": "Income",
  "amount": 1800.00,
  "currency": "BRL",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250011-c812-4c22-9014-99859f123456",
  "transactionDate": "2026-07-06T18:00:00Z"
}

```


* **Comportamento & Validações:**
  1. **Validação de Entrada:** Valida se a moeda (`currency`) é estritamente `"BRL"`. Se for diferente, rejeita a operação retornando `HTTP 400 Bad Request`.
  2. **Instanciação Segura:** Instancia os Value Objects internos (como `TransactionDescription` e `Money`) validando os formatos e restrições de valores[cite: 1, 2, 4, 5].
  3. **Verificação de Conta:** Busca a conta (`accountId`) no banco. Se não existir, retorna `HTTP 404 Not Found`. Se pertencer a outro usuário, retorna `HTTP 403 Forbidden`. Se estiver arquivada (`IsDeleted == true`), impede a operação retornando `HTTP 400 Bad Request` com o erro "Não é possível associar transações a uma conta arquivada".
  4. **Verificação de Categoria:** Busca a categoria (`categoryId`) no banco. Se não existir, retorna `HTTP 404 Not Found`. Se for pessoal e pertencer a outro usuário, retorna `HTTP 403 Forbidden`. Se estiver arquivada (`IsDeleted == true`), impede a operação retornando `HTTP 400 Bad Request` com o erro "Não é possível associar transações a uma categoria arquivada".
  5. **Compatibilidade de Fluxo:** Valida se o tipo da transação (`type`) condiz com os fluxos permitidos pela categoria selecionada (`FlowType`). Caso incompatível, retorna `HTTP 400 Bad Request`.
  6. **Tratamento de Timezone:** Garante que o `transactionDate` fornecido seja convertido explicitamente para o formato UTC antes de ser persistido.
  7. **Persistência:** Salva a nova entidade vinculando o `OwnerId` ao ID do usuário autenticado.




* **Contrato de Saída (Response HTTP 201 Created):**
```json
{
  "id": "8fa85f64-5717-4562-b3fc-2c963f66af11",
  "description": "Freelance Desenvolvimento Dashboard",
  "type": "Income",
  "money": {
    "amount": 1800.00,
    "currency": "BRL"
  },
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250011-c812-4c22-9014-99859f123456",
  "transactionDate": "2026-07-06T18:00:00Z"
}

```



### 3.4. Atualizar Transação (`PUT /transactions/{id}`)

Permite ao usuário editar os dados de uma transação de sua propriedade.

* **Contrato de Entrada (Request Body):**
```json
{
  "description": "Freelance Desenvolvimento Dashboard (Ajustado)",
  "amount": 1950.00,
  "currency": "BRL",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250011-c812-4c22-9014-99859f123456",
  "transactionDate": "2026-07-06T18:00:00Z"
}

```


* **Comportamento & Validações:**
  1. **Busca Inicial:** Busca a transação pelo ID. Se não existir ou estiver arquivada (`IsDeleted == true`), retorna `HTTP 404 Not Found`.
  2. **Verificação de Permissão:** Verifica as permissões de edição: se `OwnerId.Value != UserId`, retorna `HTTP 403 Forbidden`.
  3. **Validação de Moeda:** Valida se a moeda (`currency`) é estritamente `"BRL"`. Se for diferente, rejeita a operação retornando `HTTP 400 Bad Request`.
  4. **Revalidação de Conta/Categoria:** Caso a conta ou categoria tenham sido modificadas no corpo da requisição (ou mesmo que mantidas, para garantir segurança):
    * A conta de destino deve existir, pertencer ao usuário autenticado e **não estar arquivada (`IsDeleted == false`)**. Se estiver arquivada, rejeita retornando `HTTP 400 Bad Request`.
    * A categoria de destino deve existir, ser acessível ao usuário e **não estar arquivada (`IsDeleted == false`)**. Se estiver arquivada, rejeita retornando `HTTP 400 Bad Request`.
    * Deve-se revalidar a compatibilidade de fluxo (`FlowType`) da transação com a categoria de destino.
  5. **Tratamento de Timezone:** Garante que o `transactionDate` fornecido seja convertido explicitamente para UTC antes da atualização.
  6. **Atualização e Persistência:** Executa a atualização por meio do método de domínio `Update` e persiste as alterações.
  7. **Desbloqueio de Saldo Inicial:** Se a transação foi transferida de uma Conta A para uma Conta B, o sistema deve verificar se a Conta A ainda possui transações ativas remanescentes. Se não possuir, o saldo inicial (`InitialBalance`) da Conta A deve voltar a ser editável automaticamente.




* **Contrato de Saída (Response HTTP 200 OK):**
```json
{
  "id": "8fa85f64-5717-4562-b3fc-2c963f66af11",
  "description": "Freelance Desenvolvimento Dashboard (Ajustado)",
  "type": "Income",
  "money": {
    "amount": 1950.00,
    "currency": "BRL"
  },
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "de250011-c812-4c22-9014-99859f123456",
  "transactionDate": "2026-07-06T18:00:00Z"
}

```



### 3.5. Excluir/Arquivar Transação (`DELETE /transactions/{id}`)

Arquiva logicamente o registro de uma transação do usuário autenticado (Soft-Delete).

* **Comportamento & Validações:**
  1. **Busca Inicial:** Busca a transação pelo ID fornecido. Se não encontrar ou se já estiver arquivada (`IsDeleted == true`), retorna `HTTP 404 Not Found`.
  2. **Verificação de Permissão:** Verifica se pertencer a outro usuário. Se sim, impede a operação e retorna `HTTP 403 Forbidden`.
  3. **Arquivamento de Domínio:** Executa o método `Archive()` na entidade de transação, atualizando seu status de arquivamento (`IsDeleted = true`) e a data de atualização.
  4. **Persistência:** Atualiza o registro no repositório.
  5. **Desbloqueio de Saldo Inicial:** Após o arquivamento, o sistema deve verificar se a conta associada a essa transação ainda possui outras transações ativas (`IsDeleted == false`) remanescentes. Caso a conta não possua mais nenhuma transação ativa vinculada, o saldo inicial (`InitialBalance`) daquela conta deve ter sua edição desbloqueada automaticamente.




* **Contrato de Saída (Response HTTP 204 No Content):**
*(Corpo vazio)*.



---

## 4. Sugestões de Melhorias e Boas Práticas (Arquitetura & UX)

A fim de enriquecer a implementação técnica da entidade de transações no módulo `Finances Setup`, as seguintes práticas são recomendadas:

1. **Indexação de Performance no PostgreSQL:**
Como a tabela de transações tende a crescer de forma muito mais acelerada do que as tabelas de suporte (contas e categorias), a criação de índices compostos no arquivo de mapeamento da Fluent API é mandatória para garantir a performance de listagens e filtros[cite: 1]:


```csharp
builder.HasIndex(t => new { t.OwnerId, t.TransactionDate });
builder.HasIndex(t => new { t.OwnerId, t.AccountId });

```


2. **Gerenciamento de Saldo Dinâmico:**
Para fins de arquitetura limpa e consistência matemática simples, o saldo atualizado de uma conta deve ser calculado somando-se algebricamente o `InitialBalance` da conta com o somatório de todas as transações ativas (`IsDeleted == false`) de `Income` (créditos) menos as transações ativas de `Expense` (débitos) associadas a ela[cite: 5].
3. **Mapeamento de Precisão Decimal:**
Para evitar problemas de arredondamento com valores monetários no PostgreSQL, configure explicitamente a precisão da coluna de valor no mapeamento do EF Core da entidade (`TransactionConfiguration`)[cite: 1]:
```csharp
builder.OwnsOne(t => t.Money, money =>
{
    money.Property(m => m.Amount)
         .HasColumnName("amount")
         .HasPrecision(18, 2)
         .IsRequired();

    money.Property(m => m.Currency)
         .HasColumnName("currency")
         .HasMaxLength(3)
         .IsRequired();
});

```

4. **Atualização dos Repositórios Existentes (`Account` e `Category`):**
Atualmente, as implementações de `HasLinkedTransactionsAsync` em `AccountRepository.cs` e `CategoryRepository.cs` retornam fixamente `Task.FromResult(false)`. Com a implementação do módulo de transações, esses repositórios **devem** ser modificados para realizar consultas reais na tabela de transações via EF Core, filtrando apenas por transações ativas (`IsDeleted == false`):
* `AccountRepository.HasLinkedTransactionsAsync(AccountId id)`:
  ```csharp
  return await _dbContext.Transactions.AnyAsync(t => t.AccountId == id && !t.IsDeleted, cancellationToken);
  ```
* `CategoryRepository.HasLinkedTransactionsAsync(CategoryId id)`:
  ```csharp
  return await _dbContext.Transactions.AnyAsync(t => t.CategoryId == id && !t.IsDeleted, cancellationToken);
  ```