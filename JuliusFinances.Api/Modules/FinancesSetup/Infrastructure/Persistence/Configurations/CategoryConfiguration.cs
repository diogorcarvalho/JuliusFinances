using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade Category para o Entity Framework Core.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Mapeia para a tabela "categories"
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        // Mapeamento de CategoryId (Strongly Typed ID) usando Value Converter
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CategoryId(value))
            .IsRequired();

        // Mapeamento de CategoryName usando Value Converter
        builder.Property(c => c.Name)
            .HasConversion(
                name => name.Value,
                value => new CategoryName(value))
            .HasMaxLength(100)
            .IsRequired();

        // Mapeamento de FlowType como string no banco de dados
        builder.Property(c => c.FlowType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Mapeamento de OwnerId (opcional) usando Value Converter
        builder.Property(c => c.OwnerId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? new OwnerId(value.Value) : null);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Filtro global para ignorar registros marcados como excluídos (Soft Delete)
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Carga de inicialização (Model Seed) com dados determinísticos e imutáveis (IDs Estáticos)
        builder.HasData(
            GlobalCategories.All.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                FlowType = c.FlowType,
                OwnerId = (OwnerId?)null,
                CreatedAt = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null,
                IsDeleted = false
            }).ToArray()
        );
    }
}
