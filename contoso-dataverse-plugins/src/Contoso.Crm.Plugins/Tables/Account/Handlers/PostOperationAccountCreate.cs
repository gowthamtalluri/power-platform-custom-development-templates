using Contoso.Crm.Plugins.Infrastructure;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 40, registered ASYNCHRONOUS. Downstream side effects must not extend the user's
    /// synchronous transaction; async keeps the form responsive and gives the platform retry semantics.
    /// </summary>
    [PluginRegistration(MessageNames.Create, AccountColumns.LogicalName, PipelineStage.PostOperation,
        IsAsync = true,
        PostImageAttributes = "name,accountnumber,contoso_risktier,contoso_riskscore,ownerid",
        Description = "Creates the onboarding task and publishes the account-created integration event.",
        WorkItem = "AB#10424")]
    public sealed class PostOperationAccountCreate : PluginBase
    {
        public PostOperationAccountCreate() { }
        public PostOperationAccountCreate(string unsecure, string secure) : base(unsecure, secure) { }

        protected override void Execute(ILocalPluginContext context)
        {
            var account = context.PostImage ?? context.Target;

            var task = new Microsoft.Xrm.Sdk.Entity("task")
            {
                ["subject"] = "Complete onboarding for " + account.GetAttributeValue<string>(AccountColumns.Name),
                ["regardingobjectid"] = account.ToEntityReference(),
                ["scheduledend"] = System.DateTime.UtcNow.AddDays(3)
            };

            context.UserService.Create(task);
            context.Trace("Onboarding task created.");
        }
    }
}
