using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Extensions;
using Contoso.Crm.Plugins.Shared.Repositories;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 40 on Update. Demonstrates a controlled write back to a RELATED table
    /// and cooperative recursion suppression.
    /// </summary>
    [PluginRegistration(MessageNames.Update, AccountColumns.LogicalName, PipelineStage.PostOperation,
        FilteringAttributes = AccountColumns.OwnerId,
        PreImageAttributes = "ownerid,name",
        PostImageAttributes = "ownerid,name",
        Description = "Cascades account ownership change to active child contacts.",
        WorkItem = "AB#10425")]
    public sealed class PostOperationAccountUpdate : PluginBase
    {
        public PostOperationAccountUpdate() { }
        public PostOperationAccountUpdate(string unsecure, string secure) : base(unsecure, secure) { }

        protected override void Execute(ILocalPluginContext context)
        {
            var newOwner = context.PostImage.Get<Microsoft.Xrm.Sdk.EntityReference>(AccountColumns.OwnerId);
            var oldOwner = context.PreImage.Get<Microsoft.Xrm.Sdk.EntityReference>(AccountColumns.OwnerId);

            if (newOwner == null || newOwner.Id == oldOwner?.Id)
            {
                context.Trace("Owner unchanged; nothing to cascade.");
                return;
            }

            ExecutionGuard.Suppress(context, typeof(PostOperationAccountUpdate));

            var repository = new AccountRepository(context, context.UserService);
            var contacts = repository.GetActiveChildContacts(context.ExecutionContext.PrimaryEntityId, "contactid");

            context.Trace("Cascading ownership to {0} contact(s).", contacts.Count);

            foreach (var contact in contacts)
            {
                context.UserService.Execute(new Microsoft.Crm.Sdk.Messages.AssignRequest
                {
                    Target = contact.ToEntityReference(),
                    Assignee = newOwner
                });
            }
        }
    }
}
