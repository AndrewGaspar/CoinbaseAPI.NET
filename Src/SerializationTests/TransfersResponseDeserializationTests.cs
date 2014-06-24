using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class TransfersResponseDeserializationTests
    {
        string json = @"{
                          'transfers': [
                            {
                              'transfer': {
                                'type': 'Buy',
                                'code': 'QPCUCZHR',
                                'created_at': '2013-02-27T23:28:18-08:00',
                                'fees': {
                                  'coinbase': {
                                    'cents': 14,
                                    'currency_iso': 'USD'
                                  },
                                  'bank': {
                                    'cents': 15,
                                    'currency_iso': 'USD'
                                  }
                                },
                                'payout_date': '2013-03-05T18:00:00-08:00',
                                'transaction_id': '5011f33df8182b142400000e',
                                'status': 'Pending',
                                'btc': {
                                  'amount': '1.00000000',
                                  'currency': 'BTC'
                                },
                                'subtotal': {
                                  'amount': '13.55',
                                  'currency': 'USD'
                                },
                                'total': {
                                  'amount': '13.84',
                                  'currency': 'USD'
                                },
                                'description': 'Paid for with $13.84 from Test xxxxx3111.'
                              }
                            }
                          ],
                          'total_count': 1,
                          'num_pages': 1,
                          'current_page': 1
                        }";

        [TestMethod]
        public void TestTransfersDeserialization()
        {
            var res = JsonConvert.DeserializeObject<TransfersResponse>(json);

            Assert.AreEqual(res.TotalCount, 1);
            Assert.AreEqual(res.NumPages, 1);
            Assert.AreEqual(res.CurrentPage, 1);

            Assert.AreEqual(res.Transfers.Count, 1);

            Assert.AreEqual(res.Transfers[0].Transfer.Type, TransferType.Buy);
            Assert.AreEqual(res.Transfers[0].Transfer.Status, TransferStatus.Pending);
        }
    }
}
