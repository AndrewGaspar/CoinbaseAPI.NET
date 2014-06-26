using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Bitlet.Coinbase.Utilities
{
    public static class UriExtensions
    {
        public static Uri WithQuery(this Uri baseUri, IReadOnlyDictionary<string, string> queryParameters)
        {
            Contract.Requires(baseUri != null && queryParameters != null);

            var coll = new HttpValueCollection();
            foreach (var kvp in queryParameters)
            {
                coll.Add(kvp.Key, kvp.Value);
            }

            return new UriBuilder(baseUri)
            {
                Query = coll.ToString()
            }.Uri;
        }
    }
}
