using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade Transaction para o Entity Framework Core.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        // Mapeia para a tabela "transactions"
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        // Mapeamento de TransactionId (Strongly Typed ID) usando Value Converter
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TransactionId(value))
            .IsRequired();

        // Mapeamento de TransactionDescription usando Value Converter
        builder.Property(t => t.Description)
            .HasConversion(
                description => description.Value,
                value => new TransactionDescription(value))
            .HasMaxLength(250)
            .IsRequired();

        // Mapeamento de TransactionType como string no banco de dados
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Mapeamento de Money usando Owned Entity Types
        builder.OwnsOne(t => t.Money, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("amount")
                 .HasPrecision(18, 2)
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Mapeamento de AccountId usando Value Converter
        builder.Property(t => t.AccountId)
            .HasConversion(
                id => id.Value,
                value => new AccountId(value))
            .IsRequired();

        // Mapeamento de CategoryId usando Value Converter
        builder.Property(t => t.CategoryId)
            .HasConversion(
                id => id.Value,
                value => new CategoryId(value))
            .IsRequired();

        // Mapeamento de OwnerId usando Value Converter
        builder.Property(t => t.OwnerId)
            .HasConversion(
                id => id.Value,
                value => new OwnerId(value))
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Filtro global para ignorar registros marcados como excluídos (Soft Delete)
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Configuração dos índices de performance compostos no PostgreSQL
        builder.HasIndex(t => new { t.OwnerId, t.TransactionDate })
            .HasDatabaseName("idx_transactions_owner_date");

        builder.HasIndex(t => new { t.OwnerId, t.AccountId })
            .HasDatabaseName("idx_transactions_owner_account");
    }
}
