using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public static class Compare
    {
        public readonly static double TOLERANCE = 1E-15;

        public static bool AlmostEqual(double a, double b, double tolerance)
        {
            //if (Abs(x - y) <= epsilon * Max(Abs(x), Abs(y), 1.0f))
            var diff = Math.Abs(a - b);
            var max = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1.0f);
            return diff <= tolerance * max;
        }

        public static bool AlmostEqual(double a, double b)
        {
            return AlmostEqual(a, b, TOLERANCE);
        }

        public static bool GreaterOrEqual(double a, double b, double tolerance)
        {
            return a > b || AlmostEqual(a, b, tolerance);
        }

        public static bool LessOrEqual(double a, double b, double tolerance)
        {
            return a < b || AlmostEqual(a, b, tolerance);
        }

        public static bool Less(double a, double b, double tolerance)
        {
            return !AlmostEqual(a, b, tolerance) && a < b;
        }

        public static bool Less(double a, double b)
        {
            return !AlmostEqual(a, b, TOLERANCE) && a < b;
        }

        public static bool Greater(double a, double b, double tolerance)
        {
            return !AlmostEqual(a, b, tolerance) && a > b;
        }

        public static bool Greater(double a, double b)
        {
            return !AlmostEqual(a, b, TOLERANCE) && a > b;
        }
    }
}
