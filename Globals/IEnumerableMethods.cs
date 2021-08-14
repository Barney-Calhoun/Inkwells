using System;
using System.Collections.Generic;

namespace Globals
{
    public static class IEnumerableMethods
    {
        public static IEnumerable<int> FindIndexes<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (T item in items)
            {
                if (predicate(item))
                {
                    yield return index;
                }
                index++;
            }
        }

        public static string ToString<T>(
            this IEnumerable<T> items,
            string separator,
            string openingTag,
            string closingTag)
        {
            return $"{openingTag}{string.Join(separator, items)}{closingTag}";
        }
    }
}
