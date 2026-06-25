using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade Account para o Entity Framework Core.
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // Mapeia para a tabela "accounts"
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        // Mapeamento de AccountId (Strongly Typed ID) usando Value Converter
        builder.Property(a => a.Id)
            .HasConversion(
                id => id.Value,
                value => new AccountId(value))
            .IsRequired();

        // Mapeamento de AccountName usando Value Converter
        builder.Property(a => a.Name)
            .HasConversion(
                name => name.Value,
                value => new AccountName(value))
            .HasMaxLength(100)
            .IsRequired();

        // Mapeamento de AccountType como string no banco de dados
        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Mapeamento de InitialBalance
        builder.Property(a => a.InitialBalance)
            .IsRequired();

        // Mapeamento de OwnerId usando Value Converter
        builder.Property(a => a.OwnerId)
            .HasConversion(
                id => id.Value,
                value => new OwnerId(value))
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Filtro global para ignorar registros marcados como excluídos (Soft Delete)
        builder.HasQueryFilter(a => !a.IsDeleted);

        // Configuração do índice único filtrado (parcial) no PostgreSQL
        builder.HasIndex(a => new { a.OwnerId, a.Name })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("idx_accounts_owner_name_active");
    }
}
