using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

/// <summary>
/// Entidade de domínio rica que representa uma transferência financeira entre contas.
/// </summary>
public class Transfer
{
    public TransferId Id { get; private set; } = null!;
    public TransferDescription Description { get; private set; } = null!;
    public Money Money { get; private set; } = null!;
    public AccountId OriginAccountId { get; private set; } = null!;
    public AccountId DestinationAccountId { get; private set; } = null!;
    public CategoryId CategoryId { get; private set; } = null!;
    public OwnerId OwnerId { get; private set; } = null!;
    public DateTime TransferDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Construtor privado para materialização do EF Core
    private Transfer() { }

    /// <summary>
    /// Cria uma nova instância de transferência em estado válido.
    /// </summary>
    public Transfer(
        TransferId id,
        TransferDescription description,
        Money money,
        AccountId originAccountId,
        AccountId destinationAccountId,
        CategoryId categoryId,
        OwnerId ownerId,
        DateTime transferDate)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Money = money ?? throw new ArgumentNullException(nameof(money));
        OriginAccountId = originAccountId ?? throw new ArgumentNullException(nameof(originAccountId));
        DestinationAccountId = destinationAccountId ?? throw new ArgumentNullException(nameof(destinationAccountId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));

        if (originAccountId == destinationAccountId)
        {
            throw new DomainException("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.");
        }

        if (transferDate.Year < 2000 || transferDate.Year > 2100)
        {
            throw new DomainException("O ano da data da transferência deve estar obrigatoriamente compreendido entre o ano 2000 e o ano 2100.");
        }

        TransferDate = DateTime.SpecifyKind(transferDate, DateTimeKind.Utc);
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Atualiza os dados permitidos da transferência e preenche a data de alteração.
    /// </summary>
    public void Update(
        TransferDescription description,
        Money money,
        AccountId originAccountId,
        AccountId destinationAccountId,
        DateTime transferDate)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Money = money ?? throw new ArgumentNullException(nameof(money));
        OriginAccountId = originAccountId ?? throw new ArgumentNullException(nameof(originAccountId));
        DestinationAccountId = destinationAccountId ?? throw new ArgumentNullException(nameof(destinationAccountId));

        if (originAccountId == destinationAccountId)
        {
            throw new DomainException("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.");
        }

        if (transferDate.Year < 2000 || transferDate.Year > 2100)
        {
            throw new DomainException("O ano da data da transferência deve estar obrigatoriamente compreendido entre o ano 2000 e o ano 2100.");
        }

        TransferDate = DateTime.SpecifyKind(transferDate, DateTimeKind.Utc);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Arquiva (soft-delete) a transferência.
    /// </summary>
    public void Archive()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
