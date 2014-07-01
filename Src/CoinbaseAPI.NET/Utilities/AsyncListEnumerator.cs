using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Utilities
{
    public class AsyncListEnumerator<T> : IAsyncEnumerator<T>
    {
        protected bool AtBeginning { get; private set; }

        protected bool AtEnd { get; private set; }

        public bool Iterating { get; private set; }

        private T current;
        public T Current 
        {
            get 
            { 
                if(Iterating) 
                {
                    throw new InvalidOperationException("Attempted to retrieve current value of enumerator while mid-iteration.");
                } 
                else if(AtBeginning) 
                {
                    throw new InvalidOperationException("Attempted to retrieve current value of enumerator while at the beginning of the iteration.");
                }
                else if(AtEnd) 
                {
                    throw new InvalidOperationException("Attempted to retrieve current value of enumerator while at the end of the iteration.");
                }

                return current;
            }
            private set 
            {
                current = value;
            }
        }

        private int currentIndex = -1;
        private IAsyncReadOnlyList<T> List { get; set; }

        internal AsyncListEnumerator(IAsyncReadOnlyList<T> list)
        {
            List = list;
            Reset();
        }

        public void Reset()
        {
            if (Iterating)
            {
                throw new InvalidOperationException("Attempted to reset enumerator while iterating between values.");
            }

            AtBeginning = true;
            AtEnd = false;
            Iterating = false;
        }

        public async Task<bool> MoveNextAsync()
        {
            if (AtEnd)
            {
                return false;
            }

            try
            {
                Iterating = true;
                var count = await List.GetCountAsync();

                var index = ++currentIndex;

                if (index >= count)
                {
                    AtEnd = true;
                    return false;
                }

                Current = await List.GetItemAsync(index);
            }
            finally
            {
                AtBeginning = false;
                Iterating = false;
            }

            return true;
        }
    }
}
