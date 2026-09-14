using System;

namespace Contoso.Crm.Domain.Accounts
{
    public enum RiskTier
    {
        Low = 100_000_000,
        Medium = 100_000_001,
        High = 100_000_002,
        Prohibited = 100_000_003
    }

    public readonly struct AccountRiskSnapshot
    {
        public AccountRiskSnapshot(decimal revenue, decimal creditLimit, int employeeCount, bool isOnCreditHold)
        {
            Revenue = revenue;
            CreditLimit = creditLimit;
            EmployeeCount = employeeCount;
            IsOnCreditHold = isOnCreditHold;
        }

        public decimal Revenue { get; }
        public decimal CreditLimit { get; }
        public int EmployeeCount { get; }
        public bool IsOnCreditHold { get; }
    }

    public readonly struct AccountRiskResult
    {
        public AccountRiskResult(int score, RiskTier tier)
        {
            Score = score;
            Tier = tier;
        }

        public int Score { get; }
        public RiskTier Tier { get; }
    }

    /// <summary>
    /// Pure business rule. No Dataverse types, no I/O, fully deterministic.
    /// This is the code auditors read and the code that gets 100% test coverage in milliseconds.
    /// </summary>
    public static class AccountRiskCalculator
    {
        public static AccountRiskResult Calculate(AccountRiskSnapshot snapshot)
        {
            if (snapshot.IsOnCreditHold)
            {
                return new AccountRiskResult(0, RiskTier.Prohibited);
            }

            var exposure = snapshot.Revenue <= 0m
                ? 1m
                : Math.Min(snapshot.CreditLimit / snapshot.Revenue, 1m);

            var sizeFactor = snapshot.EmployeeCount >= 1000 ? 0.20m
                           : snapshot.EmployeeCount >= 100 ? 0.10m
                           : 0m;

            var score = (int)Math.Round((1m - exposure + sizeFactor) * 100m, MidpointRounding.AwayFromZero);
            score = Math.Max(0, Math.Min(100, score));

            var tier = score >= 70 ? RiskTier.Low
                     : score >= 40 ? RiskTier.Medium
                     : RiskTier.High;

            return new AccountRiskResult(score, tier);
        }
    }
}
