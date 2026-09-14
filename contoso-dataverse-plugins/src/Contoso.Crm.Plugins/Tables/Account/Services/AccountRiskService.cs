using System;
using Contoso.Crm.Domain.Accounts;
using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Extensions;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Tables.Account.Services
{
    /// <summary>
    /// Table-scoped behaviour shared by several Account handlers.
    /// This is the replacement for a per-table abstract base class: handlers COMPOSE it, they don't INHERIT it.
    /// Consequence: a handler only takes a dependency on the behaviour it actually uses.
    /// </summary>
    internal sealed class AccountRiskService
    {
        private readonly ILocalPluginContext _context;

        public AccountRiskService(ILocalPluginContext context) => _context = context;

        /// <summary>Columns that, when changed, require the risk tier to be recomputed.</summary>
        public static readonly string[] TriggerColumns =
        {
            AccountColumns.Revenue,
            AccountColumns.CreditLimit,
            AccountColumns.NumberOfEmployees,
            AccountColumns.CreditOnHold
        };

        public bool NeedsRescoring(Entity target) => target.AnyChanging(TriggerColumns);

        /// <summary>
        /// Applies the computed risk tier onto the in-flight Target.
        /// Called from PRE-operation so the write costs nothing extra — no second Update, no extra depth.
        /// </summary>
        public void ApplyTo(Entity target, Entity merged)
        {
            using (_context.BeginScope(nameof(AccountRiskService) + "." + nameof(ApplyTo)))
            {
                var snapshot = new AccountRiskSnapshot(
                    revenue: merged.GetMoney(AccountColumns.Revenue) ?? 0m,
                    creditLimit: merged.GetMoney(AccountColumns.CreditLimit) ?? 0m,
                    employeeCount: merged.Get<int>(AccountColumns.NumberOfEmployees),
                    isOnCreditHold: merged.Get<bool>(AccountColumns.CreditOnHold));

                var result = AccountRiskCalculator.Calculate(snapshot);

                _context.Trace("Risk computed: score={0} tier={1}", result.Score, result.Tier);

                target[AccountColumns.RiskScore] = result.Score;
                target[AccountColumns.RiskTier] = new OptionSetValue((int)result.Tier);
                target[AccountColumns.LastScoredOn] = DateTime.UtcNow;
            }
        }
    }
}
