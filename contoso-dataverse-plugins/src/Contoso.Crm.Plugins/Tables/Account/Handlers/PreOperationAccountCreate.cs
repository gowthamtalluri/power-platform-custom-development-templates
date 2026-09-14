using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Repositories;
using Contoso.Crm.Plugins.Tables.Account.Services;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 20 on Create. Mutating the Target here is free: the values persist with the original INSERT,
    /// with no additional Update call and no pipeline recursion.
    /// </summary>
    [PluginRegistration(MessageNames.Create, AccountColumns.LogicalName, PipelineStage.PreOperation,
        Description = "Validates account data and stamps the calculated risk tier before insert.",
        WorkItem = "AB#10422")]
    public sealed class PreOperationAccountCreate : PluginBase
    {
        public PreOperationAccountCreate() { }
        public PreOperationAccountCreate(string unsecure, string secure) : base(unsecure, secure) { }

        protected override bool ShouldExecute(ILocalPluginContext context) => context.Target != null;

        protected override void Execute(ILocalPluginContext context)
        {
            var target = context.Target;
            var repository = new AccountRepository(context, context.UserService);

            var validation = new AccountValidationService(context, repository);
            validation.ValidateFormat(target);
            validation.ValidateUniqueness(target, System.Guid.Empty);

            new AccountRiskService(context).ApplyTo(target, context.MergedTarget);
        }
    }
}
