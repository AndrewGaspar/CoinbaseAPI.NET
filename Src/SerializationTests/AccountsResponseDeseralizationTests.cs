using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;
using System.Linq;

namespace Bitlet.Coinbase.Tests
{
    using Models;
    using Primitives;

    [TestClass]
    public class AccountsResponseDeseralizationTests
    {
        string json =
            @"{
                'accounts': [
                {
                    'id': '536a541fa9393bb3c7000023',
                    'name': 'My Wallet',
                    'balance': {
                        'amount': '50.00000000',
                        'currency': 'BTC'
                    },
                    'native_balance': {
                        'amount': '500.12',
                        'currency': 'USD'
                    },
                    'created_at': '2014-05-07T08:41:19-07:00',
                    'primary': true,
                    'active': true
                },
                {
                    'id': '536a541fa9393bb3c7000034',
                    'name': 'Savings',
                    'balance': {
                        'amount': '0.00000000',
                        'currency': 'BTC'
                    },
                    'native_balance': {
                        'amount': '0.00',
                        'currency': 'USD'
                    },
                    'created_at': '2014-05-07T08:50:10-07:00',
                    'primary': false,
                    'active': true
                }
                ],
                'total_count': 2,
                'num_pages': 1,
                'current_page': 1
            }";

        [TestMethod]
        public void TestAccountsDeserialization()
        {
            var accounts = JsonConvert.DeserializeObject<AccountsResponse>(json);

            Assert.IsNotNull(accounts);
            Assert.IsNotNull(accounts.Accounts);

            Assert.IsTrue(accounts is PaginatedResponse);

            var accountsList = accounts.Accounts;

            Assert.AreEqual(accountsList.Count, 2);
            Assert.AreEqual(accounts.TotalCount, 2);
            Assert.AreEqual(accountsList[0].Balance.Type, typeof(Bitcoin.BTC));
            Assert.AreEqual(accountsList[0].Balance.Value, 50m);
            Assert.AreEqual(accountsList[0].CreatedAt.Year, 2014);
            Assert.AreEqual(accountsList[0].CreatedAt.Month, 5);
            Assert.AreEqual(accountsList[0].CreatedAt.Day, 7);
            Assert.IsFalse(accountsList[1].Primary);
        }
    }
}
