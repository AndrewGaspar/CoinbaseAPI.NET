using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Utilities
{
    public interface IAsyncReadOnlyList<T> : IAsyncEnumerable<T>
    {
        Task<int> GetCountAsync();

        Task<T> GetItemAsync(int index);

        Task<IList<T>> ToListAsync();
    }
}
