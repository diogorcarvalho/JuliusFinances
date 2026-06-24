using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

/// <summary>
/// Entidade de domínio rica que representa uma categoria de fluxo financeiro.
/// </summary>
public class Category
{
    public CategoryId Id { get; private set; } = null!;
    public CategoryName Name { get; private set; } = null!;
    public FlowType FlowType { get; private set; }
    public OwnerId? OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Construtor privado para materialização do EF Core
    private Category() { }

    /// <summary>
    /// Cria uma nova instância de categoria em estado válido.
    /// </summary>
    public Category(CategoryId id, CategoryName name, FlowType flowType, OwnerId? ownerId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FlowType = flowType;
        OwnerId = ownerId; // Se nulo, indica categoria Global do sistema
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Atualiza os dados da categoria e preenche a data de alteração.
    /// </summary>
    public void Update(CategoryName name, FlowType flowType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FlowType = flowType;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Arquiva (soft-delete) a categoria.
    /// </summary>
    public void Archive()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
