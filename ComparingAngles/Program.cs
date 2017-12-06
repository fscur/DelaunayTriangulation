using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleLib;

namespace ComparingAngles
{
    class Program
    {
        static double Cos(double radian)
        {
            return MathNet.Numerics.Trig.Cos(radian);
        }

        static double Sin(double radian)
        {
            return MathNet.Numerics.Trig.Sin(radian);
        }

        static double Asin(double opposite)
        {

            var a = new MathNet.Spatial.Euclidean.Line2D();
            return MathNet.Numerics.Trig.Asin(opposite);
        }

        static void Main(string[] args)
        {
            double increment = 0.0;
            double t = 1.0;
            while (t + 1.0 > 1.0)
                t *= 0.5;

            increment = t * 2.0;
            var b = increment > double.Epsilon;

            double x = 2.0;
            double y = 0.0;
            double angle = 47464716 * increment;
            var i = 0;

            while (x >= 1.0)
            {
                var sin = Sin(angle);
                var asin = Asin(sin);
                x = 1000000 * Cos(angle);
                y = 1000000 * Sin(angle);
                var a = new Vec2(x, y);
                var cross = Vec2.Cross(Vec2.XAxis, a);
                var dot = Vec2.Dot(Vec2.XAxis, a);

                Console.WriteLine(
                    string.Format(
                        "{0} - Angle({1:F30}) - Sin({2:F30}) - Cross({3:F30}) - Asin({4:F30}",
                        i++,
                        angle,
                        sin,
                        cross, 
                        asin));

                angle += increment;
            }

            //0,000000010539284334143400000000
            //0,000000010539284334143400000000
            Console.Read();
        }
    }
}
