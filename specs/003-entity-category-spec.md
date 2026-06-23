# Módulo de Configuração de Finanças - Entidade Categoria

Este módulo (Finances Setup) é responsável por gerenciar as categorias que classificam os fluxos financeiros (receitas e despesas) no JuliusFinances. Para proporcionar uma experiência flexível e rica, o sistema adota um modelo híbrido contendo categorias predefinidas de sistema (Globais) e categorias customizadas criadas pelos próprios usuários (Pessoais).

*Nota: Toda a infraestrutura técnica (tratamento global de exceções, padrões de persistência com Value Converters, gerenciamento seguro de segredos e organização de rotas de Minimal APIs) deve seguir estritamente o definido na [Especificação de Arquitetura Global](000-global-architecture-spec.md).*

---

## 1. Regras de Negócio e Comportamento

### 1.1. Visibilidade, Isolamento e Multi-inquilinato (Multi-tenant)
- **Categorias Globais:** São providas e inseridas automaticamente pelo sistema durante a carga de inicialização (Seeding). Todo usuário autenticado pode visualizá-las e vinculá-las aos seus lançamentos (receitas/despesas). Não existem endpoints na API para criação, edição ou exclusão de categorias globais; elas são somente leitura em tempo de execução.
- **Categorias Pessoais:** São criadas livremente pelos usuários autenticados. Elas são estritamente privadas e associadas ao seu respectivo dono. O Usuário A nunca poderá visualizar, utilizar, editar ou excluir as categorias pessoais do Usuário B.

### 1.2. Regra de Listagem (Leitura Unificada)
- Ao solicitar a listagem de categorias, o sistema deve unificar as categorias disponíveis para o usuário autenticado.
- **Filtro de busca:** O repositório deve buscar todas as categorias cujo `OwnerId` (Identificador do Usuário) seja igual ao ID do usuário autenticado **OU** cujo `OwnerId` seja `null` (indicando uma categoria Global).

### 1.3. Regras para Criação (Escrita)
- Toda e qualquer categoria criada via API por meio de requisições de clientes é classificada como Categoria Pessoal, sendo obrigatoriamente associada ao ID do usuário autenticado (propriedade `OwnerId`).
- **Validação de Duplicidade:** O sistema não permite que o mesmo usuário possua duas categorias ativas com o mesmo nome (comparação insensível a maiúsculas/minúsculas, espaços extras e acentuações/diacríticos).
  - O Usuário A não pode criar duas categorias ativas chamadas "Pet".
  - O Usuário A pode criar uma categoria "Pet", e o Usuário B também pode criar de forma independente.
  - O Usuário A não pode criar uma categoria pessoal ativa com o mesmo nome de uma categoria Global ativa.
  - Se o usuário possuir uma categoria pessoal inativa (arquivada/soft-deleted), ele **pode** criar uma nova categoria ativa com o mesmo nome.

### 1.4. Regras para Edição e Exclusão (Alteração)
- Antes de salvar alterações ou deletar uma categoria, o sistema deve validar se o `OwnerId` da categoria corresponde ao ID do usuário autenticado.
  - Se for idêntico: A operação prossegue.
  - Se for nulo (Global) ou pertencer a outro usuário: O sistema bloqueia a operação lançando uma exceção de domínio (que é capturada pelo middleware de exceções e mapeada para HTTP `403 Forbidden`).
- **Integridade Referencial na Exclusão:** Se o usuário tentar excluir uma categoria pessoal que já possui transações (receitas ou despesas) vinculadas, a operação deve ser impedida para evitar dados órfãos. O sistema deve retornar um erro de negócio (HTTP `400 Bad Request`) instruindo-o a reclassificar ou excluir as transações primeiro.

---

## 2. Entidade de Categoria (`Category`) e Objetos de Valor

Toda a modelagem e nomenclatura de código (classes, propriedades, métodos) do módulo deve ser escrita em **inglês**, enquanto os comentários de código devem ser em **português (pt-BR)**.

Seguindo os princípios de **Object Calisthenics** estabelecidos na Arquitetura Global, os atributos da entidade `Category` são representados por **Objetos de Valor (Value Objects)** ricos.

### 2.1. Atributos (Objetos de Valor / Propriedades)
- **Id (`CategoryId`):** Objeto de Valor (Strongly Typed ID) que envelopa o identificador único (`Guid`) da categoria.
- **Name (`CategoryName`):** Objeto de Valor que encapsula, valida e normaliza o nome da categoria.
- **FlowType (`FlowType`):** Enum do domínio que define onde a categoria pode ser utilizada.
  - `Income` (Entrada/Receita)
  - `Expense` (Saída/Despesa)
  - `Both` (Ambos os fluxos)
- **OwnerId (`OwnerId` - opcional):** Objeto de Valor local do módulo de finanças que envelopa o identificador do usuário proprietário (`Guid`). Se `null`, a categoria é Global. Isso evita acoplamento direto com as classes do domínio `Auth`.
- **CreatedAt (`DateTime`):** Data e hora de criação da categoria (UTC).
- **UpdatedAt (`DateTime?`):** Data e hora da última alteração (UTC).

### 2.2. Comportamentos dos Objetos de Valor (Value Objects)
- **`CategoryName`:**
  - **Validação:** Não pode ser nulo ou vazio; tamanho mínimo de 3 caracteres e máximo de 100 caracteres.
  - **Comportamento:** Remove espaços sobressalentes (`Trim` e remoção de múltiplos espaços internos) e padroniza a capitalização de forma elegante (ex: "alimentação" -> "Alimentação").

### 2.3. Abstrações do Domínio (`JuliusFinances.Core`)
- **`ICategoryRepository` (Interface):**
  - `Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)`: Busca uma categoria por seu ID único.
  - `Task<IEnumerable<Category>> GetByUserIdAndGlobalAsync(Guid userId, CancellationToken cancellationToken)`: Busca todas as categorias pessoais de um usuário mais as categorias globais do sistema.
  - `Task<bool> ExistsByNameAsync(CategoryName name, Guid? userId, CancellationToken cancellationToken)`: Verifica se já existe uma categoria ativa com o mesmo nome para um determinado escopo de usuário (ou global).
  - `Task<bool> HasLinkedTransactionsAsync(CategoryId id, CancellationToken cancellationToken)`: Verifica se a categoria está vinculada a alguma transação existente no banco de dados.
  - `Task AddAsync(Category category, CancellationToken cancellationToken)`: Adiciona uma nova categoria.
  - `void Update(Category category)`: Atualiza os dados de uma categoria existente.
  - `void Delete(Category category)`: Remove fisicamente (ou marca como arquivada) uma categoria.

### 2.4. Comportamentos da Entidade `Category` (Regras de Domínio)
- **Encapsulamento Total:** Todas as propriedades possuem `private set`. As modificações de estado ocorrem estritamente por meio de métodos de negócio.
- **Métodos de Domínio:**
  - `Category(CategoryId id, CategoryName name, FlowType flowType, OwnerId? ownerId)`: Construtor que garante a criação de uma categoria em estado válido.
  - `Update(CategoryName name, FlowType flowType)`: Atualiza o nome e o tipo do fluxo de caixa e preenche a propriedade `UpdatedAt`.

---

## 3. Endpoints de Categoria (Minimal APIs)

Todas as rotas do módulo de Categorias requerem autenticação JWT (`.RequireAuthorization()`). Elas serão organizadas e expostas sob o grupo de rotas `/categories` em uma classe chamada `CategoryEndpoints`.

### 3.1. Listar Categorias (`GET /categories`)
Responsável por retornar as categorias acessíveis (Globais + Pessoais) do usuário autenticado.

- **Comportamento & Validações:**
  1. Recupera o ID do usuário logado a partir das claims do token JWT.
  2. Executa a busca unificada através do repositório (`GetByUserIdAndGlobalAsync`).
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Alimentação",
      "flowType": "Expense",
      "isGlobal": true
    },
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "name": "Consultoria TI",
      "flowType": "Income",
      "isGlobal": false
    }
  ]
  ```

### 3.2. Obter Categoria por ID (`GET /categories/{id}`)
Responsável por retornar os detalhes de uma categoria específica.

- **Comportamento & Validações:**
  1. Recupera o ID do usuário logado.
  2. Busca a categoria no banco de dados por seu `id`.
  3. Caso a categoria não exista, retorna `HTTP 404 Not Found`.
  4. Caso exista, mas seja pessoal de outro usuário (`OwnerId != null` e `OwnerId != UserId`), retorna `HTTP 403 Forbidden` (utilizando Problem Details).
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "Consultoria TI",
    "flowType": "Income",
    "isGlobal": false
  }
  ```

### 3.3. Criar Categoria Pessoal (`POST /categories`)
Cria uma nova categoria pessoal associada ao usuário autenticado.

- **Contrato de Entrada (Request Body):**
  ```json
  {
    "name": "Combustível",
    "flowType": "Expense"
  }
  ```
- **Comportamento & Validações:**
  1. Valida o valor de `flowType` contra as opções permitidas do Enum.
  2. Instancia o Value Object `CategoryName` (disparando `DomainException` em caso de falha de validação - HTTP `400 Bad Request`).
  3. Verifica a duplicidade de nome chamando `ExistsByNameAsync(name, userId)`. Caso já exista uma categoria idêntica ativa (pessoal ou global), lança `CategoryNameAlreadyExistsException` (HTTP `409 Conflict`).
  4. Salva a nova entidade vinculando o `OwnerId` ao ID do usuário autenticado.
- **Contrato de Saída (Response HTTP 201 Created):**
  ```json
  {
    "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "Combustível",
    "flowType": "Expense",
    "isGlobal": false
  }
  ```

### 3.4. Atualizar Categoria (`PUT /categories/{id}`)
Permite ao usuário editar o nome ou tipo de fluxo de uma categoria pessoal de sua propriedade.

- **Contrato de Entrada (Request Body):**
  ```json
  {
    "name": "Gasolina & Álcool",
    "flowType": "Expense"
  }
  ```
- **Comportamento & Validações:**
  1. Busca a categoria pelo ID fornecido. Se não existir, retorna `HTTP 404 Not Found`.
  2. Verifica as permissões de edição: se `IsGlobal` for verdadeiro ou `OwnerId != UserId`, retorna `HTTP 403 Forbidden`.
  3. Valida se o novo nome é duplicado para o usuário (ignorando o próprio registro atual). Se for duplicado, retorna `HTTP 409 Conflict`.
  4. Executa a atualização por meio do método de domínio `Update(newName, newFlowType)` e persiste no banco de dados.
- **Contrato de Saída (Response HTTP 200 OK):**
  ```json
  {
    "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "Gasolina & Álcool",
    "flowType": "Expense",
    "isGlobal": false
  }
  ```

### 3.5. Excluir Categoria (`DELETE /categories/{id}`)
Remove uma categoria pessoal do usuário autenticado.

- **Comportamento & Validações:**
  1. Busca a categoria pelo ID fornecido. Se não encontrar, retorna `HTTP 404 Not Found`.
  2. Verifica permissões: se for Global ou pertencer a outro usuário, impede a operação e retorna `HTTP 403 Forbidden`.
  3. Verifica integridade de dados chamando `HasLinkedTransactionsAsync(id)` dentro de uma transação isolada para prevenir condições de corrida. Se houver transações vinculadas à categoria, lança uma exceção de domínio informando que a categoria possui movimentações vinculadas (HTTP `400 Bad Request`).
  4. Remove a categoria do repositório (ou executa arquivamento/soft-delete).
- **Contrato de Saída (Response HTTP 204 No Content):**
  *(Corpo vazio)*

---

## 4. Categorias Globais Pré-definidas e Carga Inicial (Seeding)

Para garantir que a aplicação seja totalmente utilizável desde o primeiro minuto, o JuliusFinances deve prover um conjunto inicial de categorias globais (com `OwnerId == null`).

### 4.1. Estratégia de Carga Inicial (Seeding)

Para a inicialização dessas categorias globais no PostgreSQL, duas abordagens são recomendadas e devem ser adotadas de forma robusta:

1. **Abordagem via Migração com Dados de Modelo (EF Core Model Seed):**
   No método `OnModelCreating` de `JuliusDbContext` (ou através de uma classe separada dedicada no pipeline de configuração), as categorias globais padrão devem ser registradas utilizando o método `.HasData()`.
   - **Requisito Obrigatório:** Todas as categorias padrão devem utilizar identificadores únicos determinísticos e imutáveis (**IDs estáticos do tipo Guid**). Isso evita que os IDs mudem entre diferentes instâncias ou execuções do sistema, permitindo que o frontend ou outros módulos façam referências fixas seguras se necessário.

2. **Abordagem via Inicializador de Startup (DB Initializer - Idempotente):**
   O pipeline de inicialização no `Program.cs` (logo após a execução do `dbContext.Database.Migrate();`) resolve um serviço de seeding (`DbInitializer.SeedCategories(dbContext)`).
   - O seeding deve ser **idempotente**. Em vez de verificar apenas a contagem geral, o serviço deve iterar sobre a lista fixa de categorias globais padrões e verificar a existência individual de cada uma delas no banco de dados por seu **ID estático Guid**. Se um ID específico não for encontrado, ele é criado. Isso garante que novas categorias globais adicionadas em deploys futuros de novas versões do software sejam inseridas sem problemas.

### 4.2. Lista de Categorias Globais Recomendadas

Abaixo está o conjunto canônico de categorias globais pré-definidas de sistema, especificando seus IDs determinísticos sugeridos, nome em português, nome técnico da constante do domínio e tipo de fluxo:

#### 4.2.1. Categorias de Saída (Despesas / Expenses)
Serão utilizadas para classificar saídas e débitos das contas do usuário.

| Nome (Exibição) | ID Sugerido (Guid) | Constante Técnica | FlowType | Descrição / Exemplos |
| :--- | :--- | :--- | :--- | :--- |
| **Alimentação** | `de250001-c812-4c22-9014-99859f123456` | `Food` | `Expense` | Supermercado, restaurantes, delivery. |
| **Habitação** | `de250002-c812-4c22-9014-99859f123456` | `Housing` | `Expense` | Aluguel, condomínio, água, luz, internet. |
| **Transporte** | `de250003-c812-4c22-9014-99859f123456` | `Transportation` | `Expense` | Combustível, transporte público, Uber/táxi. |
| **Saúde** | `de250004-c812-4c22-9014-99859f123456` | `Health` | `Expense` | Farmácia, consultas médicas, exames, plano. |
| **Educação** | `de250005-c812-4c22-9014-99859f123456` | `Education` | `Expense` | Escola, faculdade, cursos, livros. |
| **Lazer & Entretenimento** | `de250006-c812-4c22-9014-99859f123456` | `Leisure` | `Expense` | Viagens, cinema, festas, hobbies. |
| **Serviços & Assinaturas** | `de250007-c812-4c22-9014-99859f123456` | `Subscriptions` | `Expense` | Netflix, Spotify, assinaturas de software, clubes de vantagens. |
| **Outras Despesas** | `de250008-c812-4c22-9014-99859f123456` | `MiscellaneousExpenses`| `Expense` | Despesas pontuais difíceis de categorizar. |

#### 4.2.2. Categorias de Entrada (Receitas / Incomes)
Serão utilizadas para classificar entradas e créditos de recursos nas contas do usuário.

| Nome (Exibição) | ID Sugerido (Guid) | Constante Técnica | FlowType | Descrição / Exemplos |
| :--- | :--- | :--- | :--- | :--- |
| **Salário** | `de250009-c812-4c22-9014-99859f123456` | `Salary` | `Income` | Remuneração mensal, adiantamentos, bônus corporativos. |
| **Investimentos** | `de250010-c812-4c22-9014-99859f123456` | `Investments` | `Income` | Rendimento de poupança, dividendos, resgates de fundos. |
| **Freelance / Serviços** | `de250011-c812-4c22-9014-99859f123456` | `Freelance` | `Income` | Trabalhos extras, consultoria autônoma. |
| **Presentes / Prêmios** | `de250012-c812-4c22-9014-99859f123456` | `Gifts` | `Income` | Prêmios, doações, presentes em dinheiro. |
| **Outras Receitas** | `de250013-c812-4c22-9014-99859f123456` | `MiscellaneousIncomes` | `Income` | Qualquer entrada secundária não mapeada acima. |

#### 4.2.3. Categorias Especiais (Ambos / Transferências e Ajustes)
Categorias de sistema com comportamento duplo ou especial.

| Nome (Exibição) | ID Sugerido (Guid) | Constante Técnica | FlowType | Descrição / Exemplos |
| :--- | :--- | :--- | :--- | :--- |
| **Transferência** | `de250014-c812-4c22-9014-99859f123456` | `Transfer` | `Both` | Movimentação financeira entre contas do próprio usuário (ex: da conta corrente para a poupança). Não representa ganho ou perda real. |
| **Ajuste de Saldo** | `de250015-c812-4c22-9014-99859f123456` | `BalanceAdjustment`| `Both` | Ajustes de saldo manuais efetuados pelo usuário para conciliar valores físicos com o aplicativo. |

---

## 5. Sugestões de Melhorias e Boas Práticas (Arquitetura & UX)

A fim de enriquecer a implementação do módulo de categorias no JuliusFinances, as seguintes práticas de engenharia são sugeridas para adoção na camada de desenvolvimento:

1. **Desativação Temporária em vez de Exclusão Física (Soft Delete):**
   Para evitar que a exclusão de uma categoria pessoal quebre o histórico financeiro do usuário ou que a validação de integridade física seja muito custosa, sugere-se implementar a técnica de **Soft Delete** (através de uma flag `IsDeleted` ou `ArchivedAt` na entidade `Category` mapeada no EF Core como query filter global). Isso permite "arquivar" a categoria para que ela suma da lista ativa do usuário na hora de fazer novos lançamentos, mas mantenha íntegos todos os relatórios e históricos do passado.
   - **Regra para Unicidade:** O índice único do banco de dados e as queries de duplicidade devem desconsiderar os registros arquivados, permitindo que o usuário crie uma nova categoria com o mesmo nome de uma excluída anteriormente.
2. **Ícones e Cores de Identificação Visual:**
   A fim de enriquecer a apresentação em gráficos e dashboards no frontend, sugere-se adicionar dois atributos opcionais na entidade `Category`:
   - `IconKey`: Uma string (ex: `"home"`, `"shopping-cart"`, `"dollar-sign"`) que mapeia para ícones vetoriais padronizados no frontend (como Lucide Icons ou FontAwesome).
   - `HexColor`: Uma string contendo um código hexadecimal de cor (ex: `"#FF5733"`) para renderização personalizada de gráficos de pizza ou barras por categoria.
3. **Internacionalização das Categorias Globais:**
   Como as tabelas globais residem no banco com um único nome estático, sugere-se utilizar no banco de dados chaves técnicas imutáveis nos nomes, ou mapear o nome com base no idioma do cabeçalho `Accept-Language` da API se a aplicação pretender suportar multi-idiomas no futuro.
