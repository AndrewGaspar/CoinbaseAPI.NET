using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    public class TemporaryTokenProvider : ICoinbaseTokenProvider
    {
        private readonly string clientId, clientSecret;

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public DateTime ExpirationDate { get; private set; }

        public TemporaryTokenProvider(string clientId, string clientSecret, string accessToken, string refreshToken, DateTime expirationDate)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;

            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpirationDate = expirationDate;
        }

        public Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult(AccessToken);
        }

        public Task<string> GetRefreshTokenAsync()
        {
            return Task.FromResult(RefreshToken);
        }

        public Task<DateTime> GetExpirationDateAsync()
        {
            return Task.FromResult(ExpirationDate);
        }

        public async Task RefreshTokensAsync()
        {
            var response = await CoinbaseAuthentication.RefreshTokens(clientId: clientId, clientSecret: clientSecret, refreshToken: RefreshToken);

            AccessToken = response.AccessToken;
            RefreshToken = response.RefreshToken;
            ExpirationDate = DateTime.Now.AddSeconds(response.ExpiresIn);
        }
    }
}
