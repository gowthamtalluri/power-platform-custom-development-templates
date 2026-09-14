using System;
using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared;
using Contoso.Crm.Plugins.Shared.Extensions;
using Contoso.Crm.Plugins.Shared.Repositories;
using Contoso.Crm.Plugins.Tables.Account;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.CustomApis
{
    /// <summary>
    /// Custom API main-operation plugin. Custom APIs are the supported way to expose
    /// callable server-side logic to canvas apps, Power Automate and external clients —
    /// preferred over Custom Actions (workflow activities) for all new work.
    ///
    /// Request:  contoso_RecalculateAccountRisk(Target: EntityReference account)
    /// Response: RiskScore (int), RiskTier (int), Recalculated (bool)
    /// </summary>
    [PluginRegistration("contoso_RecalculateAccountRisk", "", PipelineStage.MainOperation,
        Description = "Recomputes and persists the risk tier for a single account on demand.",
        WorkItem = "AB#10430")]
    public sealed class ContosoRecalculateAccountRisk : PluginBase
    {
        public ContosoRecalculateAccountRisk() { }
        public ContosoRecalculateAccountRisk(string unsecure, string secure) : base(unsecure, secure) { }

        protected override void Execute(ILocalPluginContext context)
        {
            var target = context.GetInput<EntityReference>(ParameterNames.Target);
            Guard.AgainstNull(target, ParameterNames.Target);
            Guard.Require(target.LogicalName == AccountColumns.LogicalName,
                "This API only accepts account records.");

            var repository = new AccountRepository(context, context.UserService);
            var account = repository.GetById(target.Id,
                AccountColumns.Revenue, AccountColumns.CreditLimit,
                AccountColumns.NumberOfEmployees, AccountColumns.CreditOnHold,
                AccountColumns.RiskScore, AccountColumns.RiskTier);

            var snapshot = new Contoso.Crm.Domain.Accounts.AccountRiskSnapshot(
                account.GetMoney(AccountColumns.Revenue) ?? 0m,
                account.GetMoney(AccountColumns.CreditLimit) ?? 0m,
                account.Get<int>(AccountColumns.NumberOfEmployees),
                account.Get<bool>(AccountColumns.CreditOnHold));

            var result = Contoso.Crm.Domain.Accounts.AccountRiskCalculator.Calculate(snapshot);

            var candidate = new Entity(AccountColumns.LogicalName, target.Id)
            {
                [AccountColumns.RiskScore] = result.Score,
                [AccountColumns.RiskTier] = new OptionSetValue((int)result.Tier),
                [AccountColumns.LastScoredOn] = DateTime.UtcNow
            };

            var delta = candidate.ToDelta(account);
            var changed = delta.Attributes.Count > 0;
            repository.Update(delta);

            context.SetOutput("RiskScore", result.Score);
            context.SetOutput("RiskTier", (int)result.Tier);
            context.SetOutput("Recalculated", changed);
        }
    }
}
