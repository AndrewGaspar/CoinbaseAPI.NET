using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json;

namespace Bitlet.Coinbase.Tests
{
    using Models;

    [TestClass]
    public class UserResponseDeserializationTests
    {
        string json = @"{
          'users': [
            {
              'user': {
                'id': '512db383f8182bd24d000001',
                'name': 'User One',
                'email': 'user1@example.com',
                'time_zone': 'Pacific Time (US & Canada)',
                'native_currency': 'USD',
                'balance': {
                  'amount': '49.76000000',
                  'currency': 'BTC'
                },
                'buy_level': 1,
                'sell_level': 1,
                'buy_limit': {
                  'amount': '10.00000000',
                  'currency': 'BTC'
                },
                'sell_limit': {
                  'amount': '100.00000000',
                  'currency': 'BTC'
                }
              }
            }
          ]
        }";

        [TestMethod]
        public void TestingUsersDeserialization()
        {

            var users = JsonConvert.DeserializeObject<UsersResponse>(json);

            Assert.IsNotNull(users.Users);
            Assert.AreEqual(users.Users.Count, 1);
            Assert.IsNotNull(users.Users[0].User);
            Assert.AreEqual(users.Users[0].User.Name, "User One");
        }
    }
}