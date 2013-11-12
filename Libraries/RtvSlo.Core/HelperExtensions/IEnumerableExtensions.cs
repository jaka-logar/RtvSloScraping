using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RtvSlo.Core.HelperExtensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsEmpty(this IEnumerable<object> obj)
        {
            if (obj != null && obj.Count() > 0)
            {
                return false;
            }

            return true;
        }
    }
}
