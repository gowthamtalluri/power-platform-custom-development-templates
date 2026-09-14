using System;
using System.Globalization;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <summary>
    /// The ONE base class in this codebase that implements IPlugin.
    /// It owns cross-cutting concerns only: context construction, guard rails, telemetry, exception shaping.
    /// It contains ZERO business logic and ZERO table-specific knowledge.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        private readonly string _unsecureConfig;
        private readonly string _secureConfig;

        protected PluginBase() { }

        /// <summary>Dataverse calls this ctor when the step supplies configuration. Keep it cheap and exception-free.</summary>
        protected PluginBase(string unsecureConfig, string secureConfig)
        {
            _unsecureConfig = unsecureConfig;
            _secureConfig = secureConfig;
        }

        /// <summary>Hard stop for recursion. Registered steps should also use filtering attributes.</summary>
        protected virtual int MaxDepth => 4;

        /// <summary>Override to skip execution cheaply (attribute checks, state checks, mode checks).</summary>
        protected virtual bool ShouldExecute(ILocalPluginContext context) => true;

        /// <summary>Business entry point. Implemented by exactly one handler per registered step.</summary>
        protected abstract void Execute(ILocalPluginContext context);

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = new LocalPluginContext(serviceProvider, _unsecureConfig, _secureConfig);
            var handlerName = GetType().FullName;

            try
            {
                using (context.BeginScope(handlerName))
                {
                    context.Trace(
                        "Message={0} Entity={1} Stage={2} Mode={3} Depth={4} CorrelationId={5}",
                        context.ExecutionContext.MessageName,
                        context.ExecutionContext.PrimaryEntityName,
                        context.ExecutionContext.Stage,
                        context.ExecutionContext.Mode,
                        context.ExecutionContext.Depth,
                        context.ExecutionContext.CorrelationId);

                    if (context.ExecutionContext.Depth > MaxDepth)
                    {
                        context.Trace("Depth {0} exceeds MaxDepth {1}. Skipping to prevent recursion.",
                            context.ExecutionContext.Depth, MaxDepth);
                        return;
                    }

                    if (ExecutionGuard.IsSuppressed(context, handlerName))
                    {
                        context.Trace("Handler suppressed via shared variable. Skipping.");
                        return;
                    }

                    if (!ShouldExecute(context))
                    {
                        context.Trace("ShouldExecute returned false. Skipping.");
                        return;
                    }

                    Execute(context);
                }
            }
            catch (InvalidPluginExecutionException)
            {
                // Already a user-facing, intentional error. Let it surface untouched.
                throw;
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                context.Trace("Dataverse fault {0}: {1}", fault.Detail?.ErrorCode, fault.Detail?.Message);
                context.Logger?.LogError(fault, "Dataverse fault in {Handler}", handlerName);
                throw new InvalidPluginExecutionException(
                    OperationStatus.Failed,
                    string.Format(CultureInfo.InvariantCulture,
                        "A platform error occurred while processing this record. Reference: {0}",
                        context.ExecutionContext.CorrelationId),
                    fault);
            }
            catch (TimeoutException tex)
            {
                context.Trace("Timeout: {0}", tex.Message);
                context.Logger?.LogError(tex, "Timeout in {Handler}", handlerName);
                throw new InvalidPluginExecutionException(
                    "The operation took too long to complete. Please retry. Reference: " + context.ExecutionContext.CorrelationId,
                    tex);
            }
            catch (Exception ex)
            {
                // Never leak stack traces or internal identifiers to end users; correlate instead.
                context.Trace("Unhandled {0}: {1}", ex.GetType().Name, ex.Message);
                context.Logger?.LogError(ex, "Unhandled exception in {Handler}", handlerName);
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred. Please contact support with reference: " + context.ExecutionContext.CorrelationId,
                    ex);
            }
        }
    }
}
