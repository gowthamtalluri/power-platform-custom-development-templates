using System;
using Contoso.Crm.Plugins.Tables.Account;
using Contoso.Crm.Plugins.Tables.Account.Handlers;
using Contoso.Crm.Plugins.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Contoso.Crm.Plugins.Tests.Tables.Account
{
    public class PreOperationAccountCreateTests : PluginTestBase
    {
        private static Entity NewAccount(string number = "ACC-000123") => new Entity(AccountColumns.LogicalName, Guid.NewGuid())
        {
            [AccountColumns.Name] = "Fabrikam",
            [AccountColumns.AccountNumber] = number,
            [AccountColumns.Revenue] = new Money(1_000_000m),
            [AccountColumns.CreditLimit] = new Money(100_000m),
            [AccountColumns.NumberOfEmployees] = 50
        };

        [Fact]
        public void Stamps_RiskTier_Onto_Target_Before_Insert()
        {
            var target = NewAccount();
            var ctx = BuildContext("Create", AccountColumns.LogicalName, 20, target);

            Context.ExecutePluginWith<PreOperationAccountCreate>(ctx);

            // Asserting on the Target proves the value persists with the original INSERT (no extra Update).
            target.Should().ContainKey(AccountColumns.RiskTier);
            target.GetAttributeValue<OptionSetValue>(AccountColumns.RiskTier).Value.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Rejects_Malformed_Account_Number()
        {
            var target = NewAccount("12345");
            var ctx = BuildContext("Create", AccountColumns.LogicalName, 20, target);

            Action act = () => Context.ExecutePluginWith<PreOperationAccountCreate>(ctx);

            act.Should().Throw<InvalidPluginExecutionException>()
               .WithMessage("*is invalid*");
        }

        [Fact]
        public void Rejects_Duplicate_Account_Number()
        {
            Context.Initialize(new Entity(AccountColumns.LogicalName, Guid.NewGuid())
            {
                [AccountColumns.AccountNumber] = "ACC-000123",
                [AccountColumns.StateCode] = new OptionSetValue(AccountColumns.StateActive)
            });

            var ctx = BuildContext("Create", AccountColumns.LogicalName, 20, NewAccount());

            Action act = () => Context.ExecutePluginWith<PreOperationAccountCreate>(ctx);

            act.Should().Throw<InvalidPluginExecutionException>()
               .WithMessage("*already assigned*");
        }
    }
}
