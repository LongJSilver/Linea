using System.Collections.Generic;
using System.Text;

namespace Linea.Utils
{
    // StringBuilderPool: Provides pooling for StringBuilder instances.
    internal static class StringBuilderPool
    {
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>();
        private static readonly object _lock = new object();

        // Retrieves a StringBuilder from the pool or creates a new one if none are available.
        public static StringBuilder Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var sb = _pool.Pop();
                    sb.Clear();
                    return sb;
                }
            }
            return new StringBuilder();
        }

        // Returns a StringBuilder to the pool for reuse.
        public static void Return(StringBuilder sb)
        {
            if (sb == null) return;
            sb.Clear();
            lock (_lock)
            {
                _pool.Push(sb);
            }
        }

        // Optionally, returns the current count of pooled StringBuilders.
        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }
    }
}
