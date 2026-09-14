using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Repositories;
using Contoso.Crm.Plugins.Tables.Account.Services;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 20 on Update. FilteringAttributes is mandatory: without it this fires on every
    /// column change in the system, including background and integration writes.
    /// </summary>
    [PluginRegistration(MessageNames.Update, AccountColumns.LogicalName, PipelineStage.PreOperation,
        FilteringAttributes = AccountColumns.AccountNumber + "," + AccountColumns.Revenue + "," +
                              AccountColumns.CreditLimit + "," + AccountColumns.NumberOfEmployees + "," +
                              AccountColumns.CreditOnHold,
        PreImageAttributes = "name,accountnumber,revenue,creditlimit,creditonhold,numberofemployees,statecode,statuscode,ownerid,contoso_risktier,contoso_riskscore",
        Description = "Re-validates and re-scores the account when financial columns change.",
        WorkItem = "AB#10423")]
    public sealed class PreOperationAccountUpdate : PluginBase
    {
        public PreOperationAccountUpdate() { }
        public PreOperationAccountUpdate(string unsecure, string secure) : base(unsecure, secure) { }

        protected override bool ShouldExecute(ILocalPluginContext context) => context.Target != null;

        protected override void Execute(ILocalPluginContext context)
        {
            var target = context.Target;
            var repository = new AccountRepository(context, context.UserService);

            var validation = new AccountValidationService(context, repository);
            validation.ValidateFormat(target);
            validation.ValidateUniqueness(target, target.Id);

            var risk = new AccountRiskService(context);
            if (risk.NeedsRescoring(target))
            {
                risk.ApplyTo(target, context.MergedTarget);
            }
        }
    }
}
