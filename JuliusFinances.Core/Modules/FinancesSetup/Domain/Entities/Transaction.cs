using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

/// <summary>
/// Entidade de domínio rica que representa uma transação financeira.
/// </summary>
public class Transaction
{
    public TransactionId Id { get; private set; } = null!;
    public TransactionDescription Description { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public Money Money { get; private set; } = null!;
    public AccountId AccountId { get; private set; } = null!;
    public CategoryId CategoryId { get; private set; } = null!;
    public OwnerId OwnerId { get; private set; } = null!;
    public DateTime TransactionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Construtor privado para materialização do EF Core
    private Transaction() { }

    /// <summary>
    /// Cria uma nova instância de transação em estado válido.
    /// </summary>
    public Transaction(
        TransactionId id,
        TransactionDescription description,
        TransactionType type,
        Money money,
        AccountId accountId,
        CategoryId categoryId,
        OwnerId ownerId,
        DateTime transactionDate,
        FlowType categoryFlowType)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Type = type;
        Money = money ?? throw new ArgumentNullException(nameof(money));
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));

        ValidateFlowTypeCompatibility(type, categoryFlowType);

        TransactionDate = DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Atualiza os dados permitidos da transação e preenche a data de alteração.
    /// </summary>
    public void Update(
        TransactionDescription description,
        Money money,
        AccountId accountId,
        CategoryId categoryId,
        DateTime transactionDate,
        FlowType categoryFlowType)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Money = money ?? throw new ArgumentNullException(nameof(money));
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));

        ValidateFlowTypeCompatibility(Type, categoryFlowType);

        TransactionDate = DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Arquiva (soft-delete) a transação.
    /// </summary>
    public void Archive()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateFlowTypeCompatibility(TransactionType type, FlowType categoryFlowType)
    {
        if (type == TransactionType.Expense && categoryFlowType != FlowType.Expense && categoryFlowType != FlowType.Both)
        {
            throw new DomainException("O tipo da transação é incompatível com o tipo de fluxo permitido pela categoria.");
        }

        if (type == TransactionType.Income && categoryFlowType != FlowType.Income && categoryFlowType != FlowType.Both)
        {
            throw new DomainException("O tipo da transação é incompatível com o tipo de fluxo permitido pela categoria.");
        }
    }
}
