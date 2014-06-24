using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class ContactsResponseDeserializationTests
    {
        string json = @"{
                          'contacts': [
                            {
                              'contact': {
                                'email': 'user1@example.com'
                              }
                            },
                            {
                              'contact': {
                                'email': 'user2@example.com'
                              }
                            }
                          ],
                          'total_count': 2,
                          'num_pages': 1,
                          'current_page': 1
                        }";

        [TestMethod]
        public void TestContactsDeserialization()
        {
            var res = JsonConvert.DeserializeObject<ContactsResponse>(json);

            Assert.AreEqual(res.TotalCount, 2);
            Assert.AreEqual(res.NumPages, 1);
            Assert.AreEqual(res.CurrentPage, 1);

            Assert.AreEqual(res.Contacts.Count, 2);

            Assert.AreEqual(res.Contacts[0].Contact.Email, "user1@example.com");

            Assert.AreEqual(res.Contacts[1].Contact.Email, "user2@example.com");
        }
    }
}
