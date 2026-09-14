using System;

namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <summary>
    /// Declarative registration metadata. The code is the source of truth; CI reads these
    /// attributes to generate/verify the step manifest so registration drift is caught at build time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class PluginRegistrationAttribute : Attribute
    {
        public PluginRegistrationAttribute(string message, string primaryEntity, PipelineStage stage)
        {
            Message = message;
            PrimaryEntity = primaryEntity;
            Stage = stage;
        }

        public string Message { get; }
        public string PrimaryEntity { get; }
        public PipelineStage Stage { get; }

        /// <summary>Comma-separated logical names. Empty means "all attributes" — almost always a bug on Update.</summary>
        public string FilteringAttributes { get; set; }

        public bool IsAsync { get; set; }
        public bool DeleteAsyncOperationIfSuccessful { get; set; } = true;
        public int ExecutionOrder { get; set; } = 1;
        public string PreImageAttributes { get; set; }
        public string PostImageAttributes { get; set; }
        public string Description { get; set; }

        /// <summary>Link to the requirement / user story this step implements.</summary>
        public string WorkItem { get; set; }
    }
}
