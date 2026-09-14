using System;
using System.Text.RegularExpressions;

namespace Contoso.Crm.Domain.Accounts
{
    public static class AccountNumberPolicy
    {
        public const string ExpectedFormat = "ACC-###### (e.g. ACC-004512)";

        private static readonly Regex Pattern =
            new Regex(@"^ACC-\d{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        public static bool IsValid(string accountNumber) =>
            !string.IsNullOrWhiteSpace(accountNumber) && Pattern.IsMatch(accountNumber);

        public static string Normalize(string accountNumber) =>
            string.IsNullOrWhiteSpace(accountNumber) ? accountNumber : accountNumber.Trim().ToUpperInvariant();
    }
}
