using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

namespace JuliusFinances.Api.Common.Database;

/// <summary>
/// Responsável pela inicialização e carga inicial de dados (seeding) de forma idempotente.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Realiza a carga inicial idempotente de categorias globais de sistema no banco de dados.
    /// </summary>
    public static void SeedCategories(JuliusDbContext dbContext)
    {
        var changesMade = false;

        foreach (var category in GlobalCategories.All)
        {
            // Ignora filtros de consulta para garantir idempotência mesmo que algum registro
            // tenha sido arquivado/soft-deleted manualmente no banco
            var exists = dbContext.Categories
                .IgnoreQueryFilters()
                .Any(c => c.Id == category.Id);

            if (!exists)
            {
                dbContext.Categories.Add(category);
                changesMade = true;
            }
        }

        if (changesMade)
        {
            dbContext.SaveChanges();
        }
    }
}
