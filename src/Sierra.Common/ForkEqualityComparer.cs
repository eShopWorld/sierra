namespace Sierra.Common
{
    using Sierra.Model;
    using System;
    using System.Collections.Generic;

    public class ForkEqualityComparer : IEqualityComparer<Fork>
    {
        public bool Equals(Fork x, Fork y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return String.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Fork obj)
        {
            return obj.GetHashCode();
        }
    }
}
