namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

/// <summary>
/// Define a natureza da conta financeira.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Conta Corrente.
    /// </summary>
    CheckingAccount,

    /// <summary>
    /// Conta Poupança.
    /// </summary>
    SavingsAccount,

    /// <summary>
    /// Conta de Investimentos.
    /// </summary>
    Investment,

    /// <summary>
    /// Dinheiro em Espécie / Carteira Física.
    /// </summary>
    Cash
}
