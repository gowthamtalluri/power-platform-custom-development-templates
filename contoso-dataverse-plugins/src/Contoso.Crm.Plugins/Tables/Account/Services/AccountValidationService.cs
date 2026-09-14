using System;
using Contoso.Crm.Domain.Accounts;
using Contoso.Crm.Plugins.Infrastructure;
using Contoso.Crm.Plugins.Shared;
using Contoso.Crm.Plugins.Shared.Extensions;
using Contoso.Crm.Plugins.Shared.Repositories;
using Microsoft.Xrm.Sdk;

namespace Contoso.Crm.Plugins.Tables.Account.Services
{
    internal sealed class AccountValidationService
    {
        private readonly ILocalPluginContext _context;
        private readonly IAccountRepository _repository;

        public AccountValidationService(ILocalPluginContext context, IAccountRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        /// <summary>Format rules are pure functions in the Domain project — no Dataverse round-trip, instant to test.</summary>
        public void ValidateFormat(Entity target)
        {
            if (target.IsChanging(AccountColumns.AccountNumber))
            {
                var number = target.Get<string>(AccountColumns.AccountNumber);
                Guard.Require(AccountNumberPolicy.IsValid(number),
                    $"Account number '{number}' is invalid. Expected format: {AccountNumberPolicy.ExpectedFormat}.");
            }

            if (target.IsChanging(AccountColumns.CreditLimit) && target.IsChanging(AccountColumns.Revenue))
            {
                var limit = target.GetMoney(AccountColumns.CreditLimit) ?? 0m;
                var revenue = target.GetMoney(AccountColumns.Revenue) ?? 0m;
                Guard.Against(limit > revenue * 2,
                    "Credit limit cannot exceed twice the annual revenue. Request an exception from Finance.");
            }
        }

        /// <summary>Uniqueness needs a query, so it is isolated and only invoked when the column is actually changing.</summary>
        public void ValidateUniqueness(Entity target, Guid currentAccountId)
        {
            if (!target.IsChanging(AccountColumns.AccountNumber)) return;

            var number = target.Get<string>(AccountColumns.AccountNumber);
            if (string.IsNullOrWhiteSpace(number)) return;

            using (_context.BeginScope("ValidateUniqueness"))
            {
                Guard.Against(_repository.AccountNumberExists(number, currentAccountId),
                    $"Account number '{number}' is already assigned to another active account.");
            }
        }

        public void ValidateDeletable(Guid accountId)
        {
            var openOpportunities = _repository.CountOpenOpportunities(accountId);
            Guard.Against(openOpportunities > 0,
                $"This account cannot be deleted because it has {openOpportunities} open opportunity record(s). Close or reassign them first.");
        }
    }
}
