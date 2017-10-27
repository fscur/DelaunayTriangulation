using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    internal static class Compare
    {
        static readonly double EPSILON = 0.5E-9;
        static double RelativeError(double a, double b)
        {
            if (Compare.Equals(b, 0.0))
                return Math.Abs(a);

            return Math.Abs((a - b) / b);
        }
        public static bool AlmostEqual(double a, double b)
        {
            return a == b || RelativeError(a, b) < EPSILON;
        }

        public static bool GreaterOrEqual(double a, double b)
        {
            return a > b || AlmostEqual(a, b);
        }

        public static bool LessOrEqual(double a, double b)
        {
            return a < b || AlmostEqual(a, b);
        }

        public static bool Less(double a, double b)
        {
            return !AlmostEqual(a, b) && a < b;
        }

        public static bool Greater(double a, double b)
        {
            return !AlmostEqual(a, b) && a > b;
        }
    }
}
