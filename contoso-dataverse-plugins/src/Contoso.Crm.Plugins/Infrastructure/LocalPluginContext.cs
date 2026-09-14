using System;
using System.Diagnostics;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <inheritdoc cref="ILocalPluginContext"/>
    internal sealed class LocalPluginContext : ILocalPluginContext
    {
        private readonly Lazy<IOrganizationService> _userService;
        private readonly Lazy<IOrganizationService> _elevatedService;
        private Entity _merged;

        public LocalPluginContext(IServiceProvider serviceProvider, string unsecureConfig, string secureConfig)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            ExecutionContext = (IPluginExecutionContext4)serviceProvider.GetService(typeof(IPluginExecutionContext4));
            Tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            Logger = (ILogger)serviceProvider.GetService(typeof(ILogger));
            Notification = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
            ServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Services are lazy: a plugin that never calls Dataverse never pays the channel cost.
            _userService = new Lazy<IOrganizationService>(() => ServiceFactory.CreateOrganizationService(ExecutionContext.UserId));
            _elevatedService = new Lazy<IOrganizationService>(() => ServiceFactory.CreateOrganizationService(null));

            UnsecureConfig = unsecureConfig;
            SecureConfig = secureConfig;
        }

        public IPluginExecutionContext4 ExecutionContext { get; }
        public IOrganizationService UserService => _userService.Value;
        public IOrganizationService ElevatedService => _elevatedService.Value;
        public ITracingService Tracing { get; }
        public ILogger Logger { get; }
        public IServiceEndpointNotificationService Notification { get; }
        public IOrganizationServiceFactory ServiceFactory { get; }
        public string UnsecureConfig { get; }
        public string SecureConfig { get; }

        public Entity Target =>
            ExecutionContext.InputParameters.TryGetValue(ParameterNames.Target, out var t) ? t as Entity : null;

        public EntityReference TargetReference =>
            ExecutionContext.InputParameters.TryGetValue(ParameterNames.Target, out var t) ? t as EntityReference : null;

        public Entity PreImage =>
            ExecutionContext.PreEntityImages.TryGetValue(ImageNames.PreImage, out var i) ? i : null;

        public Entity PostImage =>
            ExecutionContext.PostEntityImages.TryGetValue(ImageNames.PostImage, out var i) ? i : null;

        public Entity MergedTarget
        {
            get
            {
                if (_merged != null) return _merged;

                var target = Target;
                var pre = PreImage;

                if (target == null) return _merged = pre;
                if (pre == null) return _merged = target;

                var merged = new Entity(target.LogicalName, target.Id);
                foreach (var attr in pre.Attributes) merged[attr.Key] = attr.Value;
                foreach (var attr in target.Attributes) merged[attr.Key] = attr.Value;
                return _merged = merged;
            }
        }

        public T GetInput<T>(string name) =>
            ExecutionContext.InputParameters.TryGetValue(name, out var v) && v is T typed ? typed : default;

        public void SetOutput<T>(string name, T value) => ExecutionContext.OutputParameters[name] = value;

        public bool TryGetSharedVariable<T>(string key, out T value)
        {
            // Shared variables set in a parent pipeline land in ParentContext, not the current context.
            var ctx = (IPluginExecutionContext)ExecutionContext;
            while (ctx != null)
            {
                if (ctx.SharedVariables.TryGetValue(key, out var raw) && raw is T typed)
                {
                    value = typed;
                    return true;
                }
                ctx = ctx.ParentContext;
            }
            value = default;
            return false;
        }

        public void SetSharedVariable<T>(string key, T value) => ExecutionContext.SharedVariables[key] = value;

        public void Trace(string format, params object[] args)
        {
            if (Tracing == null) return;
            Tracing.Trace(args == null || args.Length == 0 ? format : string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args));
        }

        public IDisposable BeginScope(string operationName) => new TraceScope(this, operationName);

        public ColumnSet Columns(params string[] columns) => new ColumnSet(columns);

        private sealed class TraceScope : IDisposable
        {
            private readonly ILocalPluginContext _ctx;
            private readonly string _name;
            private readonly Stopwatch _sw;

            public TraceScope(ILocalPluginContext ctx, string name)
            {
                _ctx = ctx;
                _name = name;
                _sw = Stopwatch.StartNew();
                _ctx.Trace("-> {0}", name);
            }

            public void Dispose()
            {
                _sw.Stop();
                _ctx.Trace("<- {0} ({1} ms)", _name, _sw.ElapsedMilliseconds);
            }
        }
    }
}
