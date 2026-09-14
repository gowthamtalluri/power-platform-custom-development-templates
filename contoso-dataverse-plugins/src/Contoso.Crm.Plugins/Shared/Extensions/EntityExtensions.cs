using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Shared.Extensions
{
    public static class EntityExtensions
    {
        public static T Get<T>(this Entity entity, string attribute) =>
            entity != null && entity.Contains(attribute) && entity[attribute] is T value ? value : default;

        public static bool Has(this Entity entity, string attribute) =>
            entity != null && entity.Contains(attribute) && entity[attribute] != null;

        /// <summary>True when the attribute is actually part of this write (not just present on an image).</summary>
        public static bool IsChanging(this Entity target, string attribute) =>
            target != null && target.Contains(attribute);

        public static bool AnyChanging(this Entity target, params string[] attributes) =>
            target != null && attributes.Any(target.Contains);

        public static Guid GetId(this Entity entity, string attribute) =>
            entity.Get<EntityReference>(attribute)?.Id ?? Guid.Empty;

        public static int? GetOptionSetValue(this Entity entity, string attribute) =>
            entity.Get<OptionSetValue>(attribute)?.Value;

        public static decimal? GetMoney(this Entity entity, string attribute) =>
            entity.Get<Money>(attribute)?.Value;

        /// <summary>Builds a minimal update payload — only send what actually changed.</summary>
        public static Entity ToDelta(this Entity candidate, Entity current)
        {
            var delta = new Entity(candidate.LogicalName, candidate.Id);
            foreach (var attr in candidate.Attributes)
            {
                var currentValue = current != null && current.Contains(attr.Key) ? current[attr.Key] : null;
                if (!AreEqual(currentValue, attr.Value)) delta[attr.Key] = attr.Value;
            }
            return delta;
        }

        public static bool AreEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;

            switch (left)
            {
                case EntityReference l when right is EntityReference r:
                    return l.Id == r.Id && string.Equals(l.LogicalName, r.LogicalName, StringComparison.Ordinal);
                case OptionSetValue l when right is OptionSetValue r:
                    return l.Value == r.Value;
                case Money l when right is Money r:
                    return l.Value == r.Value;
                default:
                    return left.Equals(right);
            }
        }

        public static bool HasAnyChange(this Entity delta, IEnumerable<string> attributes) =>
            delta != null && attributes.Any(delta.Contains);
    }
}
