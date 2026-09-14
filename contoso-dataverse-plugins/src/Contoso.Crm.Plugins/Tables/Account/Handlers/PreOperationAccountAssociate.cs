using System.Linq;
using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Tables.Account.Handlers
{
    /// <summary>
    /// Associate/Disassociate have no Target entity — the payload is Target(EntityReference),
    /// Relationship and RelatedEntities. Always filter on the relationship name first.
    /// </summary>
    [PluginRegistration(MessageNames.Associate, "", PipelineStage.PreOperation,
        Description = "Enforces the maximum number of marketing lists an account may join.",
        WorkItem = "AB#10428")]
    public sealed class PreOperationAccountAssociate : PluginBase
    {
        private const string RelationshipName = "listaccount_association";
        private const int MaxLists = 25;

        public PreOperationAccountAssociate() { }
        public PreOperationAccountAssociate(string unsecure, string secure) : base(unsecure, secure) { }

        protected override bool ShouldExecute(ILocalPluginContext context)
        {
            var relationship = context.GetInput<Relationship>(ParameterNames.Relationship);
            return relationship != null
                   && relationship.SchemaName == RelationshipName
                   && context.TargetReference?.LogicalName == AccountColumns.LogicalName;
        }

        protected override void Execute(ILocalPluginContext context)
        {
            var related = context.GetInput<EntityReferenceCollection>(ParameterNames.RelatedEntities);
            context.Trace("Associating {0} list(s).", related?.Count ?? 0);

            Guard.Against(related != null && related.Count > MaxLists,
                $"An account can be added to at most {MaxLists} marketing lists in a single operation.");
        }
    }
}
