using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Repositories;
using Contoso.Crm.Plugins.Tables.Account.Services;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 10. Runs OUTSIDE the database transaction — the correct place to reject a delete,
    /// because the platform has not yet started cascading child deletions.
    /// </summary>
    [PluginRegistration(MessageNames.Delete, AccountColumns.LogicalName, PipelineStage.PreValidation,
        PreImageAttributes = AccountColumns.Name,
        Description = "Blocks deletion of accounts that still have open opportunities.",
        WorkItem = "AB#10421")]
    public sealed class PreValidateAccountDelete : PluginBase
    {
        public PreValidateAccountDelete() { }
        public PreValidateAccountDelete(string unsecure, string secure) : base(unsecure, secure) { }

        protected override bool ShouldExecute(ILocalPluginContext context) => context.TargetReference != null;

        protected override void Execute(ILocalPluginContext context)
        {
            var repository = new AccountRepository(context, context.UserService);
            var validation = new AccountValidationService(context, repository);

            validation.ValidateDeletable(context.TargetReference.Id);
        }
    }
}
