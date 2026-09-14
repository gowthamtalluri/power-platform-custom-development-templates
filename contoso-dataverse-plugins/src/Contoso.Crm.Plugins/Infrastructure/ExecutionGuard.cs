using System;

namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <summary>
    /// Cooperative re-entrancy control. A handler that intentionally writes back to its own table
    /// sets a suppression flag so the second pass short-circuits, instead of relying on Depth alone.
    /// </summary>
    public static class ExecutionGuard
    {
        private const string Prefix = "contoso.suppress.";

        public static bool IsSuppressed(ILocalPluginContext context, string handlerName) =>
            context.TryGetSharedVariable<bool>(Prefix + handlerName, out var flag) && flag;

        public static void Suppress(ILocalPluginContext context, Type handlerType) =>
            context.SetSharedVariable(Prefix + handlerType.FullName, true);
    }
}
