using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    public interface ICoinbaseTokenProvider
    {
        Task<string> GetAccessTokenAsync();

        Task<string> GetRefreshTokenAsync();

        Task<DateTime> GetExpirationDateAsync();

        Task RefreshTokensAsync();
    }
}
