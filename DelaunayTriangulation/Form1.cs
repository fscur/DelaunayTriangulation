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
using TriangleLib;

namespace Trianglex
{
    public partial class Form1 : Form
    {
        static readonly float POINT_SIZE = 5f;
        static readonly float HALF_POINT_SIZE = POINT_SIZE * 0.5f;

        Random _rand;
        Timer _timer;
        List<Vec2> _points;
        List<Vec2> _selectedPoints = new List<Vec2>();
        List<Triangle> _triangles;
        PSLG _pslg = new PSLG();
        int _totalPoints = 1;
        bool _translating = false;
        float _zoom = 1.0f;
        float _zoomInverse = 1.0f;
        Vec2 _origin = new Vec2();
        Point _lastMousePosition;

        bool _selectPoints = true;
        bool _addPoints = false;
        bool _drawPSLG = false;

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

            mutuallyExclusiveButtons = new[] { toolStripButton1, toolStripButton2 };
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
            toolStripButton1.Checked = _selectPoints;
            toolStripButton2.Checked = _addPoints;
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
            else if (_addPoints)
            {
                var halfWidth = ClientRectangle.Width * 0.5f;
                var halfHeight = ClientRectangle.Height * 0.5f;
                var p = new Vec2(e.Location.X - halfWidth, -(e.Location.Y - halfHeight));

                p += _origin;
                p *= 1.0/_zoom;

                _points.Add(new Vec2(Math.Round(p.X), Math.Round(p.Y)));
                //_triangles = DelaunayTriangulation.Triangulate(_points);
            }
            else if (_selectPoints)
            {
                var halfWidth = ClientRectangle.Width * 0.5f;
                var halfHeight = ClientRectangle.Height * 0.5f;
                var p = new Vec2(e.Location.X - halfWidth, -(e.Location.Y - halfHeight));

                p += _origin;
                p *= 1.0 / _zoom;
                if (_selectedPoints.Count == 3)
                    _selectedPoints.Clear();

                foreach (var point in _points)
                {
                    if (Vec2.Length(point - p) < 5.0)
                        _selectedPoints.Add(point);
                }
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
                _zoomInverse = 1.0f / _zoom;
                _origin += p;
            }
            else
            {
                p *= _zoomInverse;
                _zoom *= 0.5f;
                _zoomInverse = 1.0f/_zoom;
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

            //for (int i = 0; i < 1; i++)
            //{
            //    _points = FillPoints(_totalPoints, bounds);
            //    _triangles = DelaunayTriangulation.Triangulate(_points);
            //}

            _points = new List<Vec2>();
            _pslg = new PSLG();
            //_points.Add(new Vec2() { X = 250.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 75.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 0.0 });


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

            /////////////////////////////////////////

            //_points.Add(new Vec2() { X = 0.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 100.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 50.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 150.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 150.0 });
            //_points.Add(new Vec2() { X = 200.0, Y = 100.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 50.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 0.0 });
            //_points.Add(new Vec2() { X = 250.0, Y = 75.0 });

            _points.Add(new Vec2() { X = 400.0, Y = -1500.0 });
            _points.Add(new Vec2() { X = 650.0, Y = -1000.0 });
            _points.Add(new Vec2() { X = 1000.0, Y = -300.0 });
            _points.Add(new Vec2() { X = 1400.0, Y = 500.0 });
            _points.Add(new Vec2() { X = 400.0, Y = 500.0 });
            _points.Add(new Vec2() { X = 400.0, Y = 0.0 });
            _points.Add(new Vec2() { X = 400.0, Y = -1000.0 });
            _points.Add(new Vec2() { X = 1000.0, Y = -50.0 });
            _points.Add(new Vec2() { X = 1000.0, Y = 0.0 });
            _points.Add(new Vec2() { X = 950.0, Y = 0.0 });

            var v00 = new Vertex(_points[0]);
            var v01 = new Vertex(_points[1]);
            var v02 = new Vertex(_points[2]);
            var v03 = new Vertex(_points[3]);
            var v04 = new Vertex(_points[4]);
            var v05 = new Vertex(_points[5]);
            var v06 = new Vertex(_points[6]);
            var v07 = new Vertex(_points[7]);
            var v08 = new Vertex(_points[8]);
            var v09 = new Vertex(_points[9]);

            _pslg.Edges.Add(new Edge(v00, v01));
            _pslg.Edges.Add(new Edge(v01, v02));
            _pslg.Edges.Add(new Edge(v02, v03));
            _pslg.Edges.Add(new Edge(v03, v04));
            _pslg.Edges.Add(new Edge(v04, v05));
            _pslg.Edges.Add(new Edge(v05, v06));
            _pslg.Edges.Add(new Edge(v06, v00));
            _pslg.Edges.Add(new Edge(v06, v01));
            _pslg.Edges.Add(new Edge(v02, v07));
            _pslg.Edges.Add(new Edge(v07, v08));
            _pslg.Edges.Add(new Edge(v08, v09));
            _pslg.Edges.Add(new Edge(v09, v05));

            //***************************
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +
            //new Vec2(100, 100) +

            //_points.Add(new Vec2(0.0, 0.0));
            //_points.Add(new Vec2(0.0, 4850.0));
            //_points.Add(new Vec2(-2600.0, 0.0));
            //_points.Add(new Vec2(-500.0, 1200.0));
            //_points.Add(new Vec2(-500.0, 1700.0));
            //_points.Add(new Vec2(-500.0, 2200.0));
            //_points.Add(new Vec2(-1000.0, 1200.0));
            //_points.Add(new Vec2(-1000.0, 2200.0));
            //_points.Add(new Vec2(-1500.0, 1200.0));
            //_points.Add(new Vec2(-1500.0, 1700.0));
            //_points.Add(new Vec2(-1500.0, 2050.0));
            //_points.Add(new Vec2(-1420.0, 2200.0));

            //var v00 = new Vertex(_points[0]);
            //var v01 = new Vertex(_points[1]);
            //var v02 = new Vertex(_points[2]);
            //var v03 = new Vertex(_points[3]);
            //var v04 = new Vertex(_points[4]);
            //var v05 = new Vertex(_points[5]);
            //var v06 = new Vertex(_points[6]);
            //var v07 = new Vertex(_points[7]);
            //var v08 = new Vertex(_points[8]);
            //var v09 = new Vertex(_points[9]);
            //var v10 = new Vertex(_points[10]);
            //var v11 = new Vertex(_points[11]);

            //var e00 = new Edge(v00, v01);
            //var e01 = new Edge(v01, v11);
            //var e02 = new Edge(v11, v10);
            //var e03 = new Edge(v10, v02);
            //var e04 = new Edge(v02, v00);
            //var e05 = new Edge(v10, v09);
            //var e06 = new Edge(v09, v08);
            //var e07 = new Edge(v08, v06);
            //var e08 = new Edge(v06, v03);
            //var e09 = new Edge(v03, v04);
            //var e10 = new Edge(v04, v05);
            //var e11 = new Edge(v05, v07);
            //var e12 = new Edge(v07, v11);

            ////v00.AddEdge(e00);
            ////v01.AddEdge(e00);
            ////v01.AddEdge(e01);
            ////v11.AddEdge(e01);
            ////v11.AddEdge(e02);
            ////v10.AddEdge(e02);
            ////v10.AddEdge(e03);
            ////v02.AddEdge(e03);
            ////v02.AddEdge(e04);
            ////v00.AddEdge(e04);
            ////v10.AddEdge(e05);
            ////v09.AddEdge(e05);
            ////v09.AddEdge(e06);
            ////v08.AddEdge(e06);
            ////v08.AddEdge(e07);
            ////v06.AddEdge(e07);
            ////v06.AddEdge(e08);
            ////v03.AddEdge(e08);
            ////v03.AddEdge(e09);
            ////v04.AddEdge(e09);
            ////v04.AddEdge(e10);
            ////v05.AddEdge(e10);
            ////v05.AddEdge(e11);
            ////v07.AddEdge(e11);
            ////v07.AddEdge(e12);
            ////v11.AddEdge(e12);

            //_pslg.Edges.Add(e00);
            //_pslg.Edges.Add(e01);
            //_pslg.Edges.Add(e02);
            //_pslg.Edges.Add(e03);
            //_pslg.Edges.Add(e04);
            //_pslg.Edges.Add(e05);
            //_pslg.Edges.Add(e06);
            //_pslg.Edges.Add(e07);
            //_pslg.Edges.Add(e08);
            //_pslg.Edges.Add(e09);
            //_pslg.Edges.Add(e10);
            //_pslg.Edges.Add(e11);
            //_pslg.Edges.Add(e12);

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
            DrawPoints(g, _points.Where(p => !_selectedPoints.Contains(p)).ToList(), Color.Blue);

            DrawPoints(g, _selectedPoints, Color.Red);

            if (_selectedPoints.Count > 1)
            {

                Triangle t = null;
                if (_selectedPoints.Count == 2)
                    t = new Triangle(new Vertex(_selectedPoints[0]), new Vertex(_selectedPoints[1]));
                else if (_selectedPoints.Count == 3)
                    t = new Triangle(new Vertex(_selectedPoints[0]), new Vertex(_selectedPoints[1]), new Vertex(_selectedPoints[2]));

                Draw(g, t, Color.Orange);
                DrawCircumcircle(g, t, Pens.Fuchsia);
            }

            if (_triangles != null)
                DrawTriangles(g, _triangles);

            if (_drawPSLG && _pslg != null)
            {
                foreach (var edge in _pslg.Edges)
                {
                    Draw(g, edge, Color.Maroon, 2.0f * _zoomInverse);
                }
            }
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

        private void DrawAxis(Graphics g, float halfWidth, float halfHeight)
        {
            var w0 = (-halfWidth + (float)_origin.X) * (1.0f / _zoom);
            var w1 = (halfWidth + (float)_origin.X) * (1.0f / _zoom);
            var h0 = (-halfHeight + (float)_origin.Y) * (1.0f / _zoom);
            var h1 = (halfHeight + (float)_origin.Y) * (1.0f / _zoom);

            g.SmoothingMode = SmoothingMode.Default;
            using (var pen = new Pen(Brushes.Black, -1))
            {
                g.DrawLine(pen, new PointF(w0, 0.0f), new PointF(w1, 0.0f));
                g.DrawLine(pen, new PointF(0.0f, h0), new PointF(0.0f, h1));
            }
            g.SmoothingMode = SmoothingMode.HighQuality;
        }

        private void DrawPoints(Graphics g, List<Vec2> points, Color color)
        {
            foreach (var point in points)
                using (var brush = new SolidBrush(color))
                    Draw(g, point, brush);
        }

        private void DrawTriangles(Graphics g, List<Triangle> triangles)
        {
            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;
            var p = new Vec2(_lastMousePosition.X - halfWidth, -(_lastMousePosition.Y - halfHeight));
            p += _origin;
            p *= 1.0 / _zoom;

            foreach (var triangle in triangles)
            {
                Draw(g, triangle, Color.Green);

                if (Triangle.Contains(triangle, p))
                    DrawCircumcircle(g, triangle, Pens.Fuchsia);
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

                var sla = Vec2.SquaredLength(a);
                var slb = Vec2.SquaredLength(b);
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
            g.FillEllipse(Brushes.Black, (float)p.X - 2.0f, (float)p.Y - 2.0f, (float)4.0f, (float)4.0f);
        }

        private void Draw(Graphics g, Vec2 point, Brush brush)
        {
            var x = HALF_POINT_SIZE * _zoomInverse;
            var y = HALF_POINT_SIZE * _zoomInverse;
            var w = POINT_SIZE * _zoomInverse;
            var h = POINT_SIZE * _zoomInverse;

            var rect = new RectangleF(
                (float)point.X - x,
                (float)point.Y - y,
                w,
                h);

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

        private void Draw(Graphics g, Triangle triangle, Color color)
        {
            if (triangle.E0 != null)
                Draw(g, triangle.E0, color);

            if (triangle.E1 != null)
                Draw(g, triangle.E1, color);

            if (triangle.E2 != null)
                Draw(g, triangle.E2, color);
        }

        private void Draw(Graphics g, Edge edge, Color color, float width = -1.0f)
        {
            var v0 = edge.V0.Position.ToPointF();
            var v1 = edge.V1.Position.ToPointF();

            using (var pen = new Pen(new SolidBrush(color), width))
                g.DrawLine(pen, v0.X, v0.Y, v1.X, v1.Y);
        }
        
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            _triangles = DelaunayTriangulation.Triangulate(_points);
            _timer.Start();
        }

        private readonly IEnumerable<ToolStripButton> mutuallyExclusiveButtons;
        
        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;

            if (toolStripButton1 == button)
            {
                _selectedPoints.Clear();
                _selectPoints = true;
                _addPoints = false;
            }
            else if (toolStripButton2 == button)
            {
                _selectPoints = false;
                _addPoints = true;
            }

            if (button != null && button.Checked &&
                mutuallyExclusiveButtons.Contains(button))
            {
                foreach (ToolStripButton item in mutuallyExclusiveButtons)
                {
                    if (item != button) item.Checked = !button.Checked;
                }
            }
        }
        
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            UpdatePoints();
            _triangles = DelaunayTriangulation.Triangulate(_points);
            _timer.Start();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            UpdatePoints();
            _triangles = ConformingDelaunayTriangulation.Triangulate(_pslg);
            _timer.Start();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            _timer.Stop();

            _points.Clear();

            if (_triangles != null)
                _triangles.Clear();

            _timer.Start();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            _drawPSLG = !_drawPSLG;
        }
    }

    public static class Vec2Extension
    {
        public static PointF ToPointF(this Vec2 p)
        {
            return new PointF((float)p.X, (float)p.Y);
        }
    }
}

