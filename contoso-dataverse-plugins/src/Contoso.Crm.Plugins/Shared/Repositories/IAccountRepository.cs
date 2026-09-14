using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Shared.Repositories
{
    /// <summary>
    /// Data access for the Account aggregate. Handlers never call IOrganizationService directly —
    /// that keeps queries reviewable in one place and makes handler tests trivial to fake.
    /// </summary>
    public interface IAccountRepository
    {
        Entity GetById(Guid accountId, params string[] columns);
        bool AccountNumberExists(string accountNumber, Guid excludeAccountId);
        IReadOnlyList<Entity> GetActiveChildContacts(Guid accountId, params string[] columns);
        int CountOpenOpportunities(Guid accountId);
        void Update(Entity delta);
    }
}
