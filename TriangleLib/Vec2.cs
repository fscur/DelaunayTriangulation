using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleLib
{
    public struct Vec2
    {
        public static Vec2 Zero = new Vec2(0.0, 0.0);
        public static Vec2 XAxis = new Vec2(1.0, 0.0);
        public static Vec2 YAxis = new Vec2(0.0, 1.0);

        public double X;
        public double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("({0:F30}; {1:F30})", X, Y);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Vec2 p = (Vec2)obj;
            return Compare.AlmostEqual(X, p.X, Compare.TOLERANCE) && Compare.AlmostEqual(Y, p.Y, Compare.TOLERANCE);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + (System.Math.Round(X)).GetHashCode();
                hash = hash * 23 + (System.Math.Round(Y)).GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Vec2 a, Vec2 b)
        {
            if (object.ReferenceEquals(a, null))
                return object.ReferenceEquals(b, null);

            return a.Equals(b);
        }

        public static bool operator !=(Vec2 a, Vec2 b)
        {
            return !(a == b);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2() { X = a.X - b.X, Y = a.Y - b.Y };
        }

        public static Vec2 operator -(Vec2 a)
        {
            return new Vec2() { X = -a.X, Y = -a.Y };
        }

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new Vec2() { X = a.X + b.X, Y = a.Y + b.Y };
        }

        public static Vec2 operator *(Vec2 a, double b)
        {
            return new Vec2() { X = a.X * b, Y = a.Y * b };
        }

        public static Vec2 operator *(double a, Vec2 b)
        {
            return new Vec2() { X = a * b.X, Y = a * b.Y };
        }

        public static Vec2 operator /(Vec2 a, double b)
        {
            return new Vec2() { X = a.X / b, Y = a.Y / b };
        }

        public static double Length(Vec2 a)
        {
            var dx = System.Math.Pow(a.X, 2.0);
            var dy = System.Math.Pow(a.Y, 2.0);

            return System.Math.Sqrt(dx + dy);
        }

        public static double SquaredLength(Vec2 a)
        {
            var dx = System.Math.Pow(a.X, 2.0);
            var dy = System.Math.Pow(a.Y, 2.0);

            return dx + dy;
        }

        public static Vec2 Normalize(Vec2 a)
        {
            return a / Length(a);
        }

        public static double Dot(Vec2 a, Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Cross(Vec2 a, Vec2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static Vec2 Perp(Vec2 a)
        {
            return new Vec2(-a.Y, a.X);
        }

        public static Vec2 Round(Vec2 v, int digits)
        {
            return new Vec2(System.Math.Round(v.X, digits), System.Math.Round(v.Y, digits));
        }
        public static Vec2 Round(Vec2 v)
        {
            return Vec2.Round(v, 8);
        }

        public static double Clamp(double v, double a, double b)
        {
            return Math.Min(Math.Max(v, a), b);
        }
    }
}

