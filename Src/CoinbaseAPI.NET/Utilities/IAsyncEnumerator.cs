using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Utilities
{
    public interface IAsyncEnumerator<T>
    {
        T Current { get; }

        Task<bool> MoveNextAsync();

        bool Iterating { get; }

        void Reset();
    }
}
