using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;

/// <summary>
/// Contrato para o repositório de gerenciamento de contas.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Busca uma conta por seu ID único.
    /// </summary>
    Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todas as contas pessoais ativas de um usuário.
    /// </summary>
    Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe uma conta ativa com o mesmo nome para um determinado usuário.
    /// </summary>
    Task<bool> ExistsByNameAsync(AccountName name, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a conta possui qualquer transação ou transferência vinculada no banco de dados.
    /// </summary>
    Task<bool> HasLinkedTransactionsAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova conta.
    /// </summary>
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de uma conta existente.
    /// </summary>
    void Update(Account account);

    /// <summary>
    /// Remove ou arquiva (soft-delete) uma conta.
    /// </summary>
    void Delete(Account account);
}
