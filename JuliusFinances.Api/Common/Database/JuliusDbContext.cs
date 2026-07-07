using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

namespace JuliusFinances.Api.Common.Database;

/// <summary>
/// Contexto principal de banco de dados do JuliusFinances (PostgreSQL).
/// </summary>
public class JuliusDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    public JuliusDbContext(DbContextOptions<JuliusDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Carrega automaticamente todas as configurações que implementam IEntityTypeConfiguration neste assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JuliusDbContext).Assembly);
    }
}
