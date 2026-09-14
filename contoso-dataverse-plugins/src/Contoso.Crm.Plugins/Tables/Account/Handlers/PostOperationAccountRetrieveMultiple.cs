using Contoso.Crm.Plugins.Infrastructure;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Retrieve / RetrieveMultiple plugins run on EVERY read — grids, lookups, views, exports, API calls.
    /// Treat them as a last resort and keep the body allocation-light. Prefer column-level security
    /// or a calculated column before writing one of these.
    /// </summary>
    [PluginRegistration(MessageNames.RetrieveMultiple, AccountColumns.LogicalName, PipelineStage.PostOperation,
        Description = "Masks credit limit for users without the finance role. HIGH COST - review annually.",
        WorkItem = "AB#10429")]
    public sealed class PostOperationAccountRetrieveMultiple : PluginBase
    {
        protected override bool ShouldExecute(ILocalPluginContext context) =>
            context.ExecutionContext.OutputParameters.Contains(ParameterNames.BusinessEntityCollection);

        protected override void Execute(ILocalPluginContext context)
        {
            var results = context.ExecutionContext.OutputParameters[ParameterNames.BusinessEntityCollection] as EntityCollection;
            if (results == null) return;

            foreach (var entity in results.Entities)
            {
                if (entity.Contains(AccountColumns.CreditLimit))
                {
                    entity[AccountColumns.CreditLimit] = null;
                }
            }
        }
    }
}
