using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;

/// <summary>
/// Contrato para o repositório de gerenciamento de categorias.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Busca uma categoria por seu ID único.
    /// </summary>
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todas as categorias pessoais de um usuário mais as categorias globais do sistema.
    /// </summary>
    Task<IEnumerable<Category>> GetByUserIdAndGlobalAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe uma categoria ativa com o mesmo nome para um determinado escopo de usuário (ou global).
    /// </summary>
    Task<bool> ExistsByNameAsync(CategoryName name, Guid? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a categoria está vinculada a alguma transação existente no banco de dados.
    /// </summary>
    Task<bool> HasLinkedTransactionsAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova categoria.
    /// </summary>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de uma categoria existente.
    /// </summary>
    void Update(Category category);

    /// <summary>
    /// Remove fisicamente (ou marca como arquivada) uma categoria.
    /// </summary>
    void Delete(Category category);
}
