using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class TransactionsResponseDeserializationTests
    {
        string json = @"{
          'current_user': {
            'id': '5011f33df8182b142400000e',
            'email': 'user2@example.com',
            'name': 'User Two'
          },
          'balance': {
            'amount': '50.00000000',
            'currency': 'BTC'
          },
          'native_balance': {
            'amount': '500.00',
            'currency': 'USD'
          },
          'total_count': 2,
          'num_pages': 1,
          'current_page': 1,
          'transactions': [
            {
              'transaction': {
                'id': '5018f833f8182b129c00002f',
                'created_at': '2012-08-01T02:34:43-07:00',
                'amount': {
                  'amount': '-1.10000000',
                  'currency': 'BTC'
                },
                'request': true,
                'status': 'pending',
                'sender': {
                  'id': '5011f33df8182b142400000e',
                  'name': 'User Two',
                  'email': 'user2@example.com'
                },
                'recipient': {
                  'id': '5011f33df8182b142400000a',
                  'name': 'User One',
                  'email': 'user1@example.com'
                }
              }
            },
            {
              'transaction': {
                'id': '5018f833f8182b129c00002e',
                'created_at': '2012-08-01T02:36:43-07:00',
                'hsh': '9d6a7d1112c3db9de5315b421a5153d71413f5f752aff75bf504b77df4e646a3',
                'amount': {
                  'amount': '-1.00000000',
                  'currency': 'BTC'
                },
                'request': false,
                'status': 'complete',
                'sender': {
                  'id': '5011f33df8182b142400000e',
                  'name': 'User Two',
                  'email': 'user2@example.com'
                },
                'recipient_address': '37muSN5ZrukVTvyVh3mT5Zc5ew9L9CBare'
              }
            }
         ]
        }";

        [TestMethod]
        public void TestTransactionsDeserialization()
        {
            var transactionsResponse = JsonConvert.DeserializeObject<TransactionsPage>(json);

            Assert.AreEqual(transactionsResponse.TotalCount, 2);
            Assert.AreEqual(transactionsResponse.NumPages, 1);
            Assert.AreEqual(transactionsResponse.CurrentUser.Name, "User Two");
            Assert.AreEqual(transactionsResponse.Balance.Value, 50M);
            Assert.AreEqual(transactionsResponse.Transactions.Count, 2);
            Assert.AreEqual(transactionsResponse.Transactions[0].Transaction.Status, TransactionStatus.Pending);
            Assert.IsNull(transactionsResponse.Transactions[0].Transaction.Hsh);
            Assert.IsNull(transactionsResponse.Transactions[1].Transaction.Recipient);
            Assert.AreEqual(transactionsResponse.Transactions[1].Transaction.Hsh, "9d6a7d1112c3db9de5315b421a5153d71413f5f752aff75bf504b77df4e646a3");
            Assert.AreEqual(transactionsResponse.Transactions[1].Transaction.RecipientAddress, "37muSN5ZrukVTvyVh3mT5Zc5ew9L9CBare");
        }
    }
}
