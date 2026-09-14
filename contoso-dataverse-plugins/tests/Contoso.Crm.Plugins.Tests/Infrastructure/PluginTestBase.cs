using System;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Plugins;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Crud;
using FakeXrmEasy.Middleware.Messages;
using FakeXrmEasy.Plugins.PluginSteps;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Tests.Infrastructure
{
    /// <summary>
    /// One place to build a realistic pipeline context so individual tests stay three lines long.
    /// </summary>
    public abstract class PluginTestBase
    {
        protected readonly IXrmFakedContext Context;

        protected PluginTestBase()
        {
            Context = MiddlewareBuilder
                .New()
                .AddCrud()
                .AddFakeMessageExecutors()
                .UseCrud()
                .UseMessages()
                .Build();
        }

        protected XrmFakedPluginExecutionContext BuildContext(
            string message,
            string entityLogicalName,
            int stage,
            Entity target = null,
            Entity preImage = null,
            Entity postImage = null)
        {
            var ctx = Context.GetDefaultPluginContext();
            ctx.MessageName = message;
            ctx.PrimaryEntityName = entityLogicalName;
            ctx.Stage = stage;
            ctx.Depth = 1;
            ctx.CorrelationId = Guid.NewGuid();

            if (target != null)
            {
                ctx.InputParameters["Target"] = target;
                ctx.PrimaryEntityId = target.Id;
            }
            if (preImage != null) ctx.PreEntityImages["PreImage"] = preImage;
            if (postImage != null) ctx.PostEntityImages["PostImage"] = postImage;

            return ctx;
        }
    }
}
