using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    internal static class Compare
    {
        public static bool AlmostEqual(double a, double b, double tolerance)
        {
            return a == b || Math.Abs(a - b) < tolerance;
        }

        public static bool AlmostEqual(double a, double b)
        {
            return a == b || Math.Abs(a - b) < 0.5E-9;
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
