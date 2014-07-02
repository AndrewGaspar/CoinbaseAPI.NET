using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;

    /// <summary>
    /// This interface represents an access method for the OAuth tokens needed for the Coinbase API.
    /// </summary>
    public interface ICoinbaseTokenProvider
    {
        /// <summary>
        /// This method returns an access token, possibly asynchronously.
        /// </summary>
        /// <returns>An access token returning task.</returns>
        Task<string> GetAccessTokenAsync();

        /// <summary>
        /// This method returns a refresh token, possibly asynchronously.
        /// </summary>
        /// <returns>A refresh token returning task.</returns>
        Task<string> GetRefreshTokenAsync();

        /// <summary>
        /// This method returns an expiration date, possibly asynchronously.
        /// </summary>
        /// <returns>An expiration date returning task.</returns>
        Task<DateTime> GetExpirationDateAsync();

        /// <summary>
        /// This method allows the token provider to save the tokens and expiration date when they change.
        /// </summary>
        /// <returns>A void Task that will complete when tokens are saved.</returns>
        Task RefreshTokensAsync();
    }

    /// <summary>
    /// An abstract implementation of ICoinbaseTokenProvider that implements refreshing tokens 
    /// via the client id and client secret provided in the constructor. Saving the tokens is 
    /// delegated to the implementing class.
    /// </summary>
    public abstract class AbstractCoinbaseTokenProvider : ICoinbaseTokenProvider
    {
        /// <summary>
        /// Gets the client id the token provider uses to refresh the OAuth tokens.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets the client secret the token provider uses to refresh the OAuth tokens.
        /// </summary>
        public string ClientSecret { get; private set; }

        /// <summary>
        /// Abstract constructor that takes the client id and client secret as parameters. Must be called by sub-classes.
        /// </summary>
        /// <param name="clientId">Coinbase client id for an application.</param>
        /// <param name="clientSecret">Coinbase client secret for an application.</param>
        protected AbstractCoinbaseTokenProvider(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        /// <summary>
        /// An abstract function that allows the user to save response
        /// </summary>
        /// <param name="authenticationResponse"></param>
        /// <returns></returns>
        public abstract Task SaveTokensAsync(AuthResponse authenticationResponse);

        public abstract Task<string> GetAccessTokenAsync();

        public abstract Task<string> GetRefreshTokenAsync();

        public abstract Task<DateTime> GetExpirationDateAsync();

        private async Task RefreshTokensAsyncImpl()
        {
            var response =
                    await CoinbaseAuthentication.RefreshTokensAsync(
                    clientId: ClientId, clientSecret: ClientSecret, 
                    refreshToken: await GetRefreshTokenAsync()).ConfigureAwait(false);

            await SaveTokensAsync(response).ConfigureAwait(false);

            refreshTask = null;
        }

        private Task refreshTask = null;
        public Task RefreshTokensAsync()
        {
            if (refreshTask == null)
            {
                refreshTask = RefreshTokensAsyncImpl();
            }

            return refreshTask;
        }
    }
}
