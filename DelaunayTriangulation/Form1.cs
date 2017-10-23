using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Trianglex
{
    public partial class Form1 : Form
    {
        static readonly float POINT_SIZE = 5f;
        static readonly float HALF_POINT_SIZE = POINT_SIZE * 0.5f;

        Random _rand;
        Timer _timer;
        List<Vec2> _points;
        List<Triangle> _triangles;

        int _totalPoints = 1;
        bool _translating = false;
        float _zoom = 1.0f;
        Vec2 _origin = new Vec2();
        Point _lastMousePosition;
        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            _timer = new Timer();
            _timer.Tick += (sender, e) =>
            {
                //this.UpdatePoints();
                this.Invalidate();
            };

            _rand = new Random((int)DateTime.Now.Ticks);
        }
        private List<Vec2> FillPoints(int pointCount, RectangleF bounds)
        {
            var points = new List<Vec2>();

            for (int i = 0; i < pointCount; i++)
            {
                var x = (_rand.NextDouble() * 2.0 - 1.0) * bounds.Width * 0.5;
                var y = (_rand.NextDouble() * 2.0 - 1.0) * bounds.Height * 0.5;
                points.Add(new Vec2() { X = Math.Round(x), Y = Math.Round(y) });
            }

            return points;
        }

        protected override void OnShown(EventArgs e)
        {
            UpdatePoints();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                _translating = true;
                _lastMousePosition = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_translating)
            {
                _origin += new Vec2(
                    _lastMousePosition.X - e.Location.X,
                    -(_lastMousePosition.Y - e.Location.Y));
            }

            _lastMousePosition = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_translating)
            {
                _translating = false;
            }
            else
            {
                var halfWidth = ClientRectangle.Width * 0.5f;
                var halfHeight = ClientRectangle.Height * 0.5f;
                var p = new Vec2(e.Location.X - halfWidth, -(e.Location.Y - halfHeight));

                p += _origin;
                p *= 1.0/_zoom;

                _points.Add(new Vec2(Math.Round(p.X), Math.Round(p.Y)));
                _triangles = DelaunayTriangulation.Triangulate(_points);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;

            var p = new Vec2(e.Location.X - halfWidth, -(e.Location.Y - halfHeight));
            p += _origin;

            if (e.Delta > 0)
            {
                _zoom *= 2.0f;
                _origin += p;
            }
            else
            {
                p *= 1.0 / _zoom;
                _zoom *= 0.5f;
                _origin -= (p * _zoom);
            }
        }

        private void UpdatePoints()
        {
            _timer.Stop();

            var halfWidth = ClientRectangle.Width * 0.5f * _zoom;
            var halfHeight = ClientRectangle.Height * 0.5f * _zoom;

            var bounds = new RectangleF(
                new PointF(-halfWidth, -halfHeight),
                new SizeF(ClientRectangle.Width, ClientRectangle.Height));

            for (int i = 0; i < 1; i++)
            {
                _points = FillPoints(_totalPoints, bounds);
                _triangles = DelaunayTriangulation.Triangulate(_points);
            }

            //_points = new List<Vec2>();
            //_points.Add(new Vec2() { X = 0.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 100.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 0.0 });

            //_points.Add(new Vec2() { X = 0.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = -100.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = -200.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 100.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 0.0, Y = 200.0 });



            //_points.Add(new Vec2() { X = -100.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 0.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 100.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 150.0, Y = 75.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 20.0 });

            //_points.Add(new Vec2() { X = 298.579715480367, Y = 245.416952565274 });
            //_points.Add(new Vec2() { X = -374.184350694616, Y = 294.943253952751 });
            //_points.Add(new Vec2() { X = 486.201923268941, Y = -264.478172028427 });
            //_points.Add(new Vec2() { X = -354.211467222409, Y = -445.793001202072 });

            ////_points.Add(new Vec2() { X = 0.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 100.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 150.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 150.0 });


            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_points == null)
                return;

            var g = e.Graphics;
            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;

            InitDrawing(g, halfWidth, halfHeight);
            DrawAxis(g, halfWidth, halfHeight);
            DrawPoints(g, _points);

            if (_triangles != null)
                DrawTriangles(g, _triangles);
        }

        private static void DrawAxis(Graphics g, float halfWidth, float halfHeight)
        {
            g.DrawLine(Pens.Black, new PointF(-halfWidth, 0.0f), new PointF(halfWidth, 0.0f));
            g.DrawLine(Pens.Black, new PointF(0.0f, -halfHeight), new PointF(0.0f, halfHeight));
        }

        private void InitDrawing(Graphics g, float halfWidth, float halfHeight)
        {
            var clientToWorld = new Matrix(
                            1.0f, 0.0f,
                            0.0f, -1.0f,
                            halfWidth, halfHeight);

            g.MultiplyTransform(clientToWorld);
            g.TranslateTransform(-(float)_origin.X, -(float)_origin.Y);
            g.ScaleTransform(_zoom, _zoom);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(Color.White);
        }

        private void DrawPoints(Graphics g, List<Vec2> points)
        {
            foreach (var point in points)
                Draw(g, point, Brushes.Red);
        }

        private void DrawTriangles(Graphics g, List<Triangle> triangles)
        {
            foreach (var triangle in triangles)
            {
                using (var pen = new Pen(Brushes.Green, -1))
                    Draw(g, triangle, pen);
                //DrawCircumcircle(g, triangle, Pens.Fuchsia);
            }
        }

        private void DrawCircumcircle(Graphics g, Triangle triangle, Pen pen)
        {
            double x;
            double y;
            double d;
            PointF p;

            if (triangle.V2 != null)
            {
                var A = triangle.V0.Position;
                var B = triangle.V1.Position;
                var C = triangle.V2.Position;

                var a = A - C;
                var b = B - C;

                var r = Vec2.Length(A - B) / (2.0 * Vec2.Cross(Vec2.Normalize(a), Vec2.Normalize(b)));

                var sla = Math.Pow(Vec2.Length(a), 2.0);
                var slb = Math.Pow(Vec2.Length(b), 2.0);
                var aa = (sla * b - slb * a);
                var bb = a;
                var cc = b;
                var dd = ((Vec2.Dot(aa, cc) * bb) - (Vec2.Dot(aa, bb) * cc)) / (2.0 * (sla * slb - Math.Pow(Vec2.Dot(a, b), 2.0)));

                p = (dd + C).ToPointF();
                x = p.X - r;
                y = p.Y - r;
                d = r * 2.0;
            }
            else
            {
                p = ((triangle.V0.Position + triangle.V1.Position) / 2.0).ToPointF();
                d = Vec2.Length(triangle.V0.Position - triangle.V1.Position);
                x = p.X - d * 0.5;
                y = p.Y - d * 0.5;
            }
            
            g.DrawEllipse(pen, (float)x, (float)y, (float)d, (float)d);
            g.FillEllipse(Brushes.Black, (float)p.X - 2.0f, (float)p.X - 2.0f, (float)4.0f, (float)4.0f);
        }

        private void Draw(Graphics g, Vec2 point, Brush brush)
        {
            var rect = new RectangleF(
                (float)point.X - HALF_POINT_SIZE,
                (float)point.Y - HALF_POINT_SIZE,
                POINT_SIZE,
                POINT_SIZE);

            g.FillEllipse(brush, rect);
            var state = g.Save();

            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;

            var clientToWorld = new Matrix(
                            1.0f, 0.0f,
                            0.0f, -1.0f,
                            0, 0);

            g.MultiplyTransform(clientToWorld);

            g.DrawString(string.Format("({0}, {1})", point.X, point.Y), this.Font, Brushes.Black, new PointF(rect.X + 6, -rect.Y - 10));

            g.Restore(state);
        }

        private void Draw(Graphics g, Triangle triangle, Pen pen)
        {
            if (triangle.E0 != null)
                Draw(g, triangle.E0, pen);

            if (triangle.E1 != null)
                Draw(g, triangle.E1, pen);

            if (triangle.E2 != null)
                Draw(g, triangle.E2, pen);
        }

        private void Draw(Graphics g, Edge edge, Pen pen)
        {
            var v0 = edge.V0.Position.ToPointF();
            var v1 = edge.V1.Position.ToPointF();

            g.DrawLine(pen, v0.X, v0.Y, v1.X, v1.Y);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.UpdatePoints();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            _triangles = DelaunayTriangulation.Triangulate(_points);
            _timer.Start();
        }

        
    }
}

