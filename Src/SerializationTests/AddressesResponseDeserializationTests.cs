using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class AddressesResponseDeserializationTests
    {
        string json = @"{
                          'addresses': [
                            {
                              'address': {
                                'address': 'moLxGrqWNcnGq4A8Caq8EGP4n9GUGWanj4',
                                'callback_url': null,
                                'label': 'My Label',
                                'created_at': '2013-05-09T23:07:08-07:00'
                              }
                            },
                            {
                              'address': {
                                'address': 'mwigfecvyG4MZjb6R5jMbmNcs7TkzhUaCj',
                                'callback_url': null,
                                'label': null,
                                'created_at': '2013-05-09T17:50:37-07:00'
                              }
                            }
                          ],
                          'total_count': 2,
                          'num_pages': 1,
                          'current_page': 1
                        }";

        [TestMethod]
        public void TestAddressesDeserialization()
        {
            var res = JsonConvert.DeserializeObject<AddressesPage>(json);

            Assert.AreEqual(res.TotalCount, 2);
            Assert.AreEqual(res.NumPages, 1);
            Assert.AreEqual(res.CurrentPage, 1);

            Assert.AreEqual(res.Addresses.Count, 2);

            Assert.AreEqual(res.Addresses[0].Address.Address, "moLxGrqWNcnGq4A8Caq8EGP4n9GUGWanj4");
            Assert.AreEqual(res.Addresses[0].Address.Label, "My Label");
            Assert.AreEqual(res.Addresses[0].Address.CreatedAt.Year, 2013);

            Assert.AreEqual(res.Addresses[1].Address.Address, "mwigfecvyG4MZjb6R5jMbmNcs7TkzhUaCj");
        }
    }
}
