using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;

/// <summary>
/// Contrato para o repositório de gerenciamento de transferências.
/// </summary>
public interface ITransferRepository
{
    /// <summary>
    /// Busca uma transferência por seu ID único.
    /// </summary>
    Task<Transfer?> GetByIdAsync(TransferId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca a listagem de transferências ativas do usuário de forma paginada.
    /// </summary>
    Task<IEnumerable<Transfer>> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova transferência.
    /// </summary>
    Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de uma transferência existente.
    /// </summary>
    void Update(Transfer transfer);

    /// <summary>
    /// Arquiva logicamente uma transferência (Soft-Delete).
    /// </summary>
    void Delete(Transfer transfer);
}
