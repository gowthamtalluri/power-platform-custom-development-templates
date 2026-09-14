namespace Contoso.Crm.Plugins.Tables.Account
{
    /// <summary>
    /// Schema constants for the account table. Generated names from `pac modelbuilder`
    /// can replace this file; until then this is the only place logical names are written.
    /// </summary>
    public static class AccountColumns
    {
        public const string LogicalName = "account";

        public const string AccountId = "accountid";
        public const string Name = "name";
        public const string AccountNumber = "accountnumber";
        public const string Revenue = "revenue";
        public const string CreditLimit = "creditlimit";
        public const string CreditOnHold = "creditonhold";
        public const string NumberOfEmployees = "numberofemployees";
        public const string PrimaryContactId = "primarycontactid";
        public const string ParentAccountId = "parentaccountid";
        public const string OwnerId = "ownerid";
        public const string StateCode = "statecode";
        public const string StatusCode = "statuscode";
        public const string Telephone1 = "telephone1";
        public const string EmailAddress1 = "emailaddress1";
        public const string CustomerTypeCode = "customertypecode";

        // Custom columns
        public const string RiskTier = "contoso_risktier";
        public const string RiskScore = "contoso_riskscore";
        public const string LastScoredOn = "contoso_lastscoredon";
        public const string IntegrationKey = "contoso_integrationkey";

        public const int StateActive = 0;
        public const int StateInactive = 1;

        /// <summary>Columns every Account handler needs on its PreImage. Register images with exactly this set.</summary>
        public static readonly string[] StandardPreImage =
        {
            Name, AccountNumber, Revenue, CreditLimit, CreditOnHold,
            NumberOfEmployees, StateCode, StatusCode, OwnerId, RiskTier, RiskScore
        };
    }
}
