using System;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Shared
{
    /// <summary>Fail fast with messages an end user can act on.</summary>
    public static class Guard
    {
        public static void AgainstNull(object value, string parameterName)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
        }

        public static void Against(bool condition, string userMessage)
        {
            if (condition) throw new InvalidPluginExecutionException(OperationStatus.Failed, userMessage);
        }

        public static void Require(bool condition, string userMessage)
        {
            if (!condition) throw new InvalidPluginExecutionException(OperationStatus.Failed, userMessage);
        }
    }
}
