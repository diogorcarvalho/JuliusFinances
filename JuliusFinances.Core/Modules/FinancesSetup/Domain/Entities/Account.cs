using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

/// <summary>
/// Entidade de domínio rica que representa uma conta financeira.
/// </summary>
public class Account
{
    public AccountId Id { get; private set; } = null!;
    public AccountName Name { get; private set; } = null!;
    public AccountType Type { get; private set; }
    public decimal InitialBalance { get; private set; }
    public OwnerId OwnerId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Construtor privado para materialização do EF Core
    private Account() { }

    /// <summary>
    /// Cria uma nova instância de conta em estado válido.
    /// </summary>
    public Account(AccountId id, AccountName name, AccountType type, decimal initialBalance, OwnerId ownerId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        InitialBalance = initialBalance;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;

        if (Type == AccountType.Cash && InitialBalance < 0)
        {
            throw new DomainException("Contas do tipo 'Dinheiro em Espécie' não podem possuir saldo inicial negativo.");
        }
    }

    /// <summary>
    /// Atualiza os dados da conta e preenche a data de alteração.
    /// </summary>
    public void Update(AccountName name, AccountType type, decimal? initialBalance = null, bool hasTransactions = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;

        if (initialBalance.HasValue && initialBalance.Value != InitialBalance)
        {
            if (hasTransactions)
            {
                throw new DomainException("O saldo inicial não pode ser alterado após o registro de transações.");
            }
            InitialBalance = initialBalance.Value;
        }

        if (Type == AccountType.Cash && InitialBalance < 0)
        {
            throw new DomainException("Contas do tipo 'Dinheiro em Espécie' não podem possuir saldo inicial negativo.");
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Arquiva (soft-delete) a conta.
    /// </summary>
    public void Archive()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
