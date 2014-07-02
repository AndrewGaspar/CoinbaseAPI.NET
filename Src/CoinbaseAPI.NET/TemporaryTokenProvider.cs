using System;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;

    /// <summary>
    /// This is a CoinbaseTokenProvider implementation that stores the tokens in memory and has synchronous accessors.
    /// 
    /// This is only to be used temporarily - after enough identifying information has been received from Coinbase,
    /// these access credentials should be saved to permanent storage and AbstractCoinbaseTokenProvider should
    /// be subclassed with an implementation that updates that storage location when the access and refresh tokens change.
    /// </summary>
    public class TemporaryTokenProvider : AbstractCoinbaseTokenProvider
    {
        private readonly string clientId, clientSecret;

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public DateTime ExpirationDate { get; private set; }

        /// <summary>
        /// Constructor must be provided with initial value for the access token, refresh token, and expiration date.
        /// 
        /// This data typically would be provided CoinbaseAuthentication.RequestTokens.
        /// </summary>
        /// <param name="clientId">The application's client id.</param>
        /// <param name="clientSecret">The application's client secret.</param>
        /// <param name="accessToken">The user's OAuth access token.</param>
        /// <param name="refreshToken">The user's OAuth refresh token.</param>
        /// <param name="expirationDate">The expiration date of the user's OAuth tokens.</param>
        public TemporaryTokenProvider(string clientId, string clientSecret, string accessToken, string refreshToken, DateTime expirationDate)
            : base(clientId, clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;

            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpirationDate = expirationDate;
        }

        public override Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult(AccessToken);
        }

        public override Task<string> GetRefreshTokenAsync()
        {
            return Task.FromResult(RefreshToken);
        }

        public override Task<DateTime> GetExpirationDateAsync()
        {
            return Task.FromResult(ExpirationDate);
        }

        public override async Task SaveTokensAsync(AuthResponse response)
        {
            AccessToken = response.AccessToken;
            RefreshToken = response.RefreshToken;
            ExpirationDate = DateTime.Now.AddSeconds(response.ExpiresIn);
        }
    }
}
