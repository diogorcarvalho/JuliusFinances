using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;

/// <summary>
/// Conjunto fixo de categorias globais pré-definidas de sistema.
/// </summary>
public static class GlobalCategories
{
    public static readonly Category Food = new(
        new CategoryId(Guid.Parse("de250001-c812-4c22-9014-99859f123456")),
        new CategoryName("Alimentação"),
        FlowType.Expense,
        null);

    public static readonly Category Housing = new(
        new CategoryId(Guid.Parse("de250002-c812-4c22-9014-99859f123456")),
        new CategoryName("Habitação"),
        FlowType.Expense,
        null);

    public static readonly Category Transportation = new(
        new CategoryId(Guid.Parse("de250003-c812-4c22-9014-99859f123456")),
        new CategoryName("Transporte"),
        FlowType.Expense,
        null);

    public static readonly Category Health = new(
        new CategoryId(Guid.Parse("de250004-c812-4c22-9014-99859f123456")),
        new CategoryName("Saúde"),
        FlowType.Expense,
        null);

    public static readonly Category Education = new(
        new CategoryId(Guid.Parse("de250005-c812-4c22-9014-99859f123456")),
        new CategoryName("Educação"),
        FlowType.Expense,
        null);

    public static readonly Category Leisure = new(
        new CategoryId(Guid.Parse("de250006-c812-4c22-9014-99859f123456")),
        new CategoryName("Lazer & Entretenimento"),
        FlowType.Expense,
        null);

    public static readonly Category Subscriptions = new(
        new CategoryId(Guid.Parse("de250007-c812-4c22-9014-99859f123456")),
        new CategoryName("Serviços & Assinaturas"),
        FlowType.Expense,
        null);

    public static readonly Category MiscellaneousExpenses = new(
        new CategoryId(Guid.Parse("de250008-c812-4c22-9014-99859f123456")),
        new CategoryName("Outras Despesas"),
        FlowType.Expense,
        null);

    public static readonly Category Salary = new(
        new CategoryId(Guid.Parse("de250009-c812-4c22-9014-99859f123456")),
        new CategoryName("Salário"),
        FlowType.Income,
        null);

    public static readonly Category Investments = new(
        new CategoryId(Guid.Parse("de250010-c812-4c22-9014-99859f123456")),
        new CategoryName("Investimentos"),
        FlowType.Income,
        null);

    public static readonly Category Freelance = new(
        new CategoryId(Guid.Parse("de250011-c812-4c22-9014-99859f123456")),
        new CategoryName("Freelance / Serviços"),
        FlowType.Income,
        null);

    public static readonly Category Gifts = new(
        new CategoryId(Guid.Parse("de250012-c812-4c22-9014-99859f123456")),
        new CategoryName("Presentes / Prêmios"),
        FlowType.Income,
        null);

    public static readonly Category MiscellaneousIncomes = new(
        new CategoryId(Guid.Parse("de250013-c812-4c22-9014-99859f123456")),
        new CategoryName("Outras Receitas"),
        FlowType.Income,
        null);

    public static readonly Category Transfer = new(
        new CategoryId(Guid.Parse("de250014-c812-4c22-9014-99859f123456")),
        new CategoryName("Transferência"),
        FlowType.Both,
        null);

    public static readonly Category BalanceAdjustment = new(
        new CategoryId(Guid.Parse("de250015-c812-4c22-9014-99859f123456")),
        new CategoryName("Ajuste de Saldo"),
        FlowType.Both,
        null);

    public static readonly IReadOnlyCollection<Category> All = new[]
    {
        Food,
        Housing,
        Transportation,
        Health,
        Education,
        Leisure,
        Subscriptions,
        MiscellaneousExpenses,
        Salary,
        Investments,
        Freelance,
        Gifts,
        MiscellaneousIncomes,
        Transfer,
        BalanceAdjustment
    };
}
