using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    internal static class Compare
    {
        public readonly static double TOLERANCE = 1E-15;

        public static bool AlmostEqual(double a, double b, double tolerance)
        {
            if (a == b)
                return true;

            double epsilon = Math.Max(Math.Abs(a), Math.Abs(b)) * tolerance;
            return Math.Abs(a - b) < epsilon;
        }

        //public static bool AlmostEqual(double a, double b)
        //{
        //    return AlmostEqual(a, b, TOLERANCE);
        //}

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

        public static bool Greater(double a, double b, double tolerance)
        {
            return !AlmostEqual(a, b, tolerance) && a > b;
        }
    }
}
