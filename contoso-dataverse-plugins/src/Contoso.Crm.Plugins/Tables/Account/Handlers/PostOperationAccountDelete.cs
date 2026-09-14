using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Extensions;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Stage 40 on Delete, ASYNCHRONOUS. The row is gone — all state must come from the PreImage.
    /// </summary>
    [PluginRegistration(MessageNames.Delete, AccountColumns.LogicalName, PipelineStage.PostOperation,
        IsAsync = true,
        PreImageAttributes = "name,accountnumber,contoso_integrationkey",
        Description = "Writes an audit trail row and notifies the downstream ERP of the deletion.",
        WorkItem = "AB#10426")]
    public sealed class PostOperationAccountDelete : PluginBase
    {
        public PostOperationAccountDelete() { }
        public PostOperationAccountDelete(string unsecure, string secure) : base(unsecure, secure) { }

        protected override void Execute(ILocalPluginContext context)
        {
            var preImage = context.PreImage;
            if (preImage == null)
            {
                context.Trace("No PreImage registered; cannot audit. Check step registration.");
                return;
            }

            var audit = new Microsoft.Xrm.Sdk.Entity("contoso_deletionaudit")
            {
                ["contoso_name"] = preImage.Get<string>(AccountColumns.Name),
                ["contoso_recordid"] = context.ExecutionContext.PrimaryEntityId.ToString(),
                ["contoso_tablename"] = AccountColumns.LogicalName,
                ["contoso_deletedby"] = new Microsoft.Xrm.Sdk.EntityReference("systemuser", context.ExecutionContext.InitiatingUserId),
                ["contoso_deletedon"] = System.DateTime.UtcNow
            };

            context.ElevatedService.Create(audit);
        }
    }
}
