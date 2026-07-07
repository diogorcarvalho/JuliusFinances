using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade Transfer para o Entity Framework Core.
/// </summary>
public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        // Mapeia para a tabela "transfers"
        builder.ToTable("transfers");

        builder.HasKey(t => t.Id);

        // Mapeamento de TransferId (Strongly Typed ID) usando Value Converter
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TransferId(value))
            .IsRequired();

        // Mapeamento de TransferDescription usando Value Converter
        builder.Property(t => t.Description)
            .HasConversion(
                description => description.Value,
                value => new TransferDescription(value))
            .HasMaxLength(250)
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

        // Mapeamento de OriginAccountId usando Value Converter
        builder.Property(t => t.OriginAccountId)
            .HasConversion(
                id => id.Value,
                value => new AccountId(value))
            .IsRequired();

        // Mapeamento de DestinationAccountId usando Value Converter
        builder.Property(t => t.DestinationAccountId)
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

        builder.Property(t => t.TransferDate)
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
        builder.HasIndex(t => new { t.OwnerId, t.TransferDate })
            .HasDatabaseName("idx_transfers_owner_date");

        builder.HasIndex(t => new { t.OwnerId, t.OriginAccountId })
            .HasDatabaseName("idx_transfers_owner_origin_account");

        builder.HasIndex(t => new { t.OwnerId, t.DestinationAccountId })
            .HasDatabaseName("idx_transfers_owner_destination_account");
    }
}
