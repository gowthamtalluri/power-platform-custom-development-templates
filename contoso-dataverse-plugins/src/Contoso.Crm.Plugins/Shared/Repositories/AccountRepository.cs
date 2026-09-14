using System;
using System.Collections.Generic;
using System.Linq;
using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Tables.Account;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Contoso.Crm.Plugins.Shared.Repositories
{
    internal sealed class AccountRepository : IAccountRepository
    {
        private readonly IOrganizationService _service;
        private readonly ILocalPluginContext _context;

        public AccountRepository(ILocalPluginContext context, IOrganizationService service)
        {
            _context = context;
            _service = service;
        }

        public Entity GetById(Guid accountId, params string[] columns) =>
            _service.Retrieve(AccountColumns.LogicalName, accountId, new ColumnSet(columns));

        public bool AccountNumberExists(string accountNumber, Guid excludeAccountId)
        {
            var query = new QueryExpression(AccountColumns.LogicalName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                NoLock = true,
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(AccountColumns.AccountNumber, ConditionOperator.Equal, accountNumber),
                        new ConditionExpression(AccountColumns.StateCode, ConditionOperator.Equal, AccountColumns.StateActive)
                    }
                }
            };

            if (excludeAccountId != Guid.Empty)
            {
                query.Criteria.AddCondition(AccountColumns.AccountId, ConditionOperator.NotEqual, excludeAccountId);
            }

            return _service.RetrieveMultiple(query).Entities.Count > 0;
        }

        public IReadOnlyList<Entity> GetActiveChildContacts(Guid accountId, params string[] columns)
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(columns),
                NoLock = true,
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("parentcustomerid", ConditionOperator.Equal, accountId),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                    }
                },
                PageInfo = new PagingInfo { Count = 500, PageNumber = 1, ReturnTotalRecordCount = false }
            };

            var results = new List<Entity>();
            while (true)
            {
                var page = _service.RetrieveMultiple(query);
                results.AddRange(page.Entities);
                if (!page.MoreRecords) break;
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
            return results;
        }

        public int CountOpenOpportunities(Guid accountId)
        {
            var query = new QueryExpression("opportunity")
            {
                ColumnSet = new ColumnSet(false),
                NoLock = true,
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("customerid", ConditionOperator.Equal, accountId),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                    }
                }
            };
            return _service.RetrieveMultiple(query).Entities.Count;
        }

        public void Update(Entity delta)
        {
            if (delta == null || delta.Attributes.Count == 0)
            {
                _context.Trace("AccountRepository.Update skipped: empty delta.");
                return;
            }
            _service.Update(delta);
        }
    }
}
