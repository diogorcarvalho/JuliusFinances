using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Api.Modules.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade User para o Entity Framework Core.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Mapeia para a tabela "users" (o UseSnakeCaseNamingConvention cuidará do padrão)
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        // Mapeamento de UserId (Strongly Typed ID) usando Value Converter
        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        // Mapeamento de Name usando Value Converter
        builder.Property(u => u.Name)
            .HasConversion(
                name => name.Value,
                value => new Name(value))
            .HasMaxLength(150)
            .IsRequired();

        // Mapeamento de Email usando Value Converter
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255)
            .IsRequired();

        // Mapeamento de Password usando Value Converter (armazenando o HashValue)
        builder.Property(u => u.Password)
            .HasConversion(
                password => password.HashValue,
                value => new Password(value))
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        // Índice Único de E-mail
        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
