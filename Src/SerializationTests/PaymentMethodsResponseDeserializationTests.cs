using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class PaymentMethodsResponseDeserializationTests
    {
        string json = @"{
                          'payment_methods': [
                            {
                              'payment_method': {
                                'id': '530eb5b217cb34e07a000011',
                                'name': 'US Bank ****4567',
                                'can_buy': true,
                                'can_sell': true
                               }
                            },
                            {
                              'payment_method': {
                                'id': '530eb7e817cb34e07a00001a',
                                'name': 'VISA card 1111',
                                'can_buy': false,
                                'can_sell': false
                               }
                            }
                          ],
                          'default_buy': '530eb5b217cb34e07a000011',
                          'default_sell': '530eb5b217cb34e07a000011'
                        }";

        [TestMethod]
        public void TestPaymentMethodsDeserialization()
        {
            var res = JsonConvert.DeserializeObject<PaymentMethodsResponse>(json);

            Assert.AreEqual(res.DefaultBuy, "530eb5b217cb34e07a000011");

            Assert.AreEqual(res.DefaultSell, "530eb5b217cb34e07a000011");

            Assert.AreEqual(res.PaymentMethods.Count, 2);

            Assert.AreEqual(res.PaymentMethods[0].PaymentMethod.Id, "530eb5b217cb34e07a000011");
            Assert.AreEqual(res.PaymentMethods[0].PaymentMethod.Name, "US Bank ****4567");
            Assert.IsTrue(res.PaymentMethods[0].PaymentMethod.CanBuy);
            Assert.IsTrue(res.PaymentMethods[0].PaymentMethod.CanSell);

            Assert.AreEqual(res.PaymentMethods[1].PaymentMethod.Id, "530eb7e817cb34e07a00001a");
            Assert.AreEqual(res.PaymentMethods[1].PaymentMethod.Name, "VISA card 1111");
            Assert.IsFalse(res.PaymentMethods[1].PaymentMethod.CanBuy);
            Assert.IsFalse(res.PaymentMethods[1].PaymentMethod.CanSell);
        }
    }
}
