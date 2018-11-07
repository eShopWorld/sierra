namespace Sierra.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// equality comparer for generic type T based on ToString() implementation
    /// </summary>
    public class ToStringEqualityComparer<T> : IEqualityComparer<T> where T:class
    {
        /// <inheritdoc/>
        public bool Equals(T x, T y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
