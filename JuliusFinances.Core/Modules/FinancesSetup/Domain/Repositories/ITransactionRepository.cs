using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;

/// <summary>
/// Contrato para o repositório de gerenciamento de transações.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Busca uma transação por seu ID único.
    /// </summary>
    Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca a listagem de transações ativas e não arquivadas do usuário de forma paginada, com filtros opcionais.
    /// </summary>
    Task<IEnumerable<Transaction>> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? accountId = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova transação.
    /// </summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de uma transação existente.
    /// </summary>
    void Update(Transaction transaction);

    /// <summary>
    /// Arquiva logicamente uma transação (Soft-Delete).
    /// </summary>
    void Delete(Transaction transaction);
}
