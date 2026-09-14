using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <summary>
    /// Everything a handler is allowed to touch. Handlers depend on this interface only,
    /// which is what makes them unit-testable without a Dataverse connection.
    /// </summary>
    public interface ILocalPluginContext
    {
        IPluginExecutionContext4 ExecutionContext { get; }

        /// <summary>Organization service running as the *initiating* user. Use for permission-respecting work.</summary>
        IOrganizationService UserService { get; }

        /// <summary>Organization service running as SYSTEM. Use only when elevation is genuinely required.</summary>
        IOrganizationService ElevatedService { get; }

        ITracingService Tracing { get; }

        /// <summary>Structured telemetry that flows to Application Insights when the environment is wired up.</summary>
        ILogger Logger { get; }

        IServiceEndpointNotificationService Notification { get; }

        IOrganizationServiceFactory ServiceFactory { get; }

        /// <summary>Unsecure configuration string supplied at step registration.</summary>
        string UnsecureConfig { get; }

        /// <summary>Secure configuration string supplied at step registration. Never trace this.</summary>
        string SecureConfig { get; }

        Entity Target { get; }
        EntityReference TargetReference { get; }
        Entity PreImage { get; }
        Entity PostImage { get; }

        /// <summary>PostImage ?? merge(PreImage, Target). The single source of truth for "what the row looks like now".</summary>
        Entity MergedTarget { get; }

        T GetInput<T>(string name);
        void SetOutput<T>(string name, T value);

        /// <summary>Per-execution bag shared across plugins in the same pipeline (ExecutionContext.SharedVariables).</summary>
        bool TryGetSharedVariable<T>(string key, out T value);
        void SetSharedVariable<T>(string key, T value);

        void Trace(string format, params object[] args);
        IDisposable BeginScope(string operationName);
        ColumnSet Columns(params string[] columns);
    }
}
