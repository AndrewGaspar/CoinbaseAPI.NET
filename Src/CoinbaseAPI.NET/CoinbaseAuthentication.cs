using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;

    using Utilities;

    public class CoinbaseAuthenticationException : Exception
    {
        public IReadOnlyDictionary<string, string> Parameters { get; private set; }
        public HttpStatusCode Code { get; private set; }

        public CoinbaseAuthenticationException(IReadOnlyDictionary<string, string> parameters, HttpStatusCode code)
        {
            Parameters = parameters;
            Code = code;
        }

        public override string ToString()
        {
            return String.Format("Coinbase authorization failed with the HTTP status code ({0}) and with these parameters: [{1}]", 
                Code, 
                String.Join(",", from parameter in Parameters select "{" + parameter.Key + "," + parameter.Value + "}"));
        }
    }

    public static class CoinbaseAuthentication
    {
        private static JsonSerializerSettings _serializerSettings
            = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        private static async Task<AuthResponse> GetTokens(IReadOnlyDictionary<string, string> parameters)
        {
            var uri = new Uri("https://coinbase.com/oauth/token").WithQuery(parameters);

            using (var client = new HttpClient())
            {
                var httpResponse = await client.PostAsync(uri, new StringContent("")).ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new CoinbaseAuthenticationException(parameters, httpResponse.StatusCode);
                }
                
                return JsonConvert.DeserializeObject<AuthResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }

        private static Dictionary<string, string> GetBasicParameters(string clientId, string clientSecret)
        {
            var parameters = new Dictionary<string, string>();
            parameters["client_id"] = clientId;
            parameters["client_secret"] = clientSecret;
            return parameters;
        }

        public static Task<AuthResponse> RequestTokens(string clientId, string clientSecret, string redirectUri, string code)
        {
            var parameters = GetBasicParameters(clientId, clientSecret);
            parameters["grant_type"] = "authorization_code";
            parameters["code"] = code;
            parameters["redirect_uri"] = redirectUri;

            return GetTokens(parameters);
        }

        public static Task<AuthResponse> RefreshTokens(string clientId, string clientSecret, string refreshToken)
        {
            var parameters = GetBasicParameters(clientId, clientSecret);
            parameters["grant_type"] = "refresh_token";
            parameters["refresh_token"] = refreshToken;

            return GetTokens(parameters);
        }
    }
}
