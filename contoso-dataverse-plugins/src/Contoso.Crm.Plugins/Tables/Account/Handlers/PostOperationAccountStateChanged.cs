using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared.Extensions;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Deactivation arrives as an Update to statecode, NOT as SetState — SetStateDynamicEntity
    /// is deprecated and no longer raised by modern clients. Register on Update + statecode filter.
    /// </summary>
    [PluginRegistration(MessageNames.Update, AccountColumns.LogicalName, PipelineStage.PostOperation,
        FilteringAttributes = AccountColumns.StateCode,
        IsAsync = true,
        PreImageAttributes = "statecode,statuscode,name",
        PostImageAttributes = "statecode,statuscode,name",
        Description = "Reacts to account activation/deactivation.",
        WorkItem = "AB#10427")]
    public sealed class PostOperationAccountStateChanged : PluginBase
    {
        public PostOperationAccountStateChanged() { }
        public PostOperationAccountStateChanged(string unsecure, string secure) : base(unsecure, secure) { }

        protected override void Execute(ILocalPluginContext context)
        {
            var newState = context.PostImage.GetOptionSetValue(AccountColumns.StateCode);
            var oldState = context.PreImage.GetOptionSetValue(AccountColumns.StateCode);

            if (newState == oldState) return;

            context.Trace("Account state changed {0} -> {1}", oldState, newState);

            if (newState == AccountColumns.StateInactive)
            {
                // e.g. close open activities, suspend integration sync
                context.Trace("Deactivation side effects executed.");
            }
        }
    }
}
