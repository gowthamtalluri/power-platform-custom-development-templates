using Contoso.Crm.Domain.Accounts;
using FluentAssertions;
using Xunit;

namespace Contoso.Crm.Domain.Tests
{
    /// <summary>
    /// Pure-rule tests. No Dataverse, no fakes, no setup. These run in milliseconds
    /// and should cover every branch of every business rule.
    /// </summary>
    public class AccountRiskCalculatorTests
    {
        [Fact]
        public void CreditHold_Always_Yields_Prohibited()
        {
            var result = AccountRiskCalculator.Calculate(
                new AccountRiskSnapshot(revenue: 10_000_000m, creditLimit: 0m, employeeCount: 5000, isOnCreditHold: true));

            result.Tier.Should().Be(RiskTier.Prohibited);
            result.Score.Should().Be(0);
        }

        [Theory]
        [InlineData(1_000_000, 100_000, 10, RiskTier.Low)]
        [InlineData(1_000_000, 700_000, 10, RiskTier.Medium)]
        [InlineData(1_000_000, 950_000, 10, RiskTier.High)]
        public void Tier_Follows_Exposure_Ratio(decimal revenue, decimal limit, int employees, RiskTier expected)
        {
            AccountRiskCalculator
                .Calculate(new AccountRiskSnapshot(revenue, limit, employees, false))
                .Tier.Should().Be(expected);
        }

        [Fact]
        public void Zero_Revenue_Is_Treated_As_Full_Exposure()
        {
            AccountRiskCalculator
                .Calculate(new AccountRiskSnapshot(0m, 50_000m, 10, false))
                .Score.Should().Be(0);
        }

        [Fact]
        public void Large_Headcount_Improves_Score()
        {
            var small = AccountRiskCalculator.Calculate(new AccountRiskSnapshot(1_000_000m, 500_000m, 10, false));
            var large = AccountRiskCalculator.Calculate(new AccountRiskSnapshot(1_000_000m, 500_000m, 5000, false));

            large.Score.Should().BeGreaterThan(small.Score);
        }
    }
}
