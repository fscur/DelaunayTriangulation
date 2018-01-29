using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TriangleLib;
using static TriangleLib.Edge;

namespace Trianglex
{
    enum EditMode
    {
        Select,
        Add,
        Move
    }

    enum AddMode
    {
        None,
        Vertex,
        ConstrainedEdge
    }

    enum ViewMode
    {
        None,
        Zooming,
        Panning
    }

    enum TriangulationMode
    {
        Delaunay,
        ConformingDelaunay
    }

    struct DrawOptions
    {
        public bool DrawPSLG;
        public bool DrawFlippableEdges;
    }

    public partial class Form1 : Form
    {
        static readonly float POINT_SIZE = 10f;
        static readonly float HALF_POINT_SIZE = POINT_SIZE * 0.5f;
        static readonly double TOLERANCE = 10.0;

        Random _rand;
        Timer _timer;

        List<Vertex> _vertices = new List<Vertex>();
        PSLG _pslg = new PSLG();

        ConformingDelaunayTriangulation _conformingTriangulation;
        DelaunayTriangulation _delaunayTriangulation;

        float _zoom = 8.0f;
        float _zoomInverse = 1.0f / 8.0f;
        Vec2 _origin = new Vec2();
        Point _lastMousePosition;

        int _movingPointIndex = -1;
        List<int> _selectedIndices = new List<int>();
        Vertex _v0;
        Edge _tempEdge;

        Edge _testEdge = new Edge(new Vertex(new Vec2(-100, 0)), new Vertex(new Vec2(100, 0)));
        List<Edge.EdgeIntersection> _testIntersections = new List<Edge.EdgeIntersection>();

        DrawOptions _drawOptions = new DrawOptions();
        EditMode _editMode = EditMode.Select;
        AddMode _addMode = AddMode.None;
        ViewMode _viewMode = ViewMode.None;
        TriangulationMode _triangulationMode = TriangulationMode.Delaunay;

        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            _addMode = AddMode.Vertex;
            _editMode = EditMode.Select;
            _drawOptions.DrawPSLG = true;

            _timer = new Timer();
            _timer.Tick += (sender, e) =>
            {
                //this.UpdatePoints();
                 this.Invalidate();
            };

            _rand = new Random((int)DateTime.Now.Ticks);
        }

        protected override void OnShown(EventArgs e)
        {
            UpdatePoints();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    HandleLeftMouseDown(e);
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    _viewMode = ViewMode.Panning;
                    _lastMousePosition = e.Location;
                    break;
                default:
                    break;
            }
        }

        private void HandleLeftMouseDown(MouseEventArgs e)
        {
            var p = SelectPoint(e.Location);

            switch (_editMode)
            {
                case EditMode.Select:
                    {
                        if (p != null)
                        {
                            if (Form.ModifierKeys == Keys.Control)
                                _selectedIndices.Add(_vertices.IndexOf(p));
                            else
                            {
                                _selectedIndices.Clear();
                                _selectedIndices.Add(_vertices.IndexOf(p));
                            }
                        }

                        break;
                    }
                case EditMode.Move:
                    if (p != null)
                        _movingPointIndex = _vertices.IndexOf(p);

                    break;
                case EditMode.Add:
                    {
                        if (_pslg == null)
                            _pslg = new PSLG();

                        if (_v0 == null)
                            _v0 = p ?? new Vertex(PointToWorld(e.Location));

                        break;
                    }
                default:
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;

            if (_viewMode == ViewMode.Panning)
            {
                _origin += new Vec2(
                    _lastMousePosition.X - e.Location.X,
                    -(_lastMousePosition.Y - e.Location.Y));
            }
            else if (_editMode == EditMode.Move)
            {
                this.Cursor = Cursors.SizeAll;

                if (_movingPointIndex == -1)
                    return;

                var p0 = PointToWorld(e.Location);
                var p1 = _vertices[_movingPointIndex];

                var dist = Math.Abs(Vec2.Length(p0 - p1.Position));

                var vertex = _vertices[_movingPointIndex];
                vertex.Position = p0;

                if (_triangulationMode == TriangulationMode.Delaunay)
                {
                    if (_vertices.Count > 2)
                        _delaunayTriangulation = DelaunayTriangulation.Triangulate(_vertices);
                }
                else if (_triangulationMode == TriangulationMode.ConformingDelaunay)
                {
                    //_conformingTriangulation = ConformingDelaunayTriangulation.Triangulate(_pslg, _vertices, TOLERANCE);

                    //_intersections = _conformingTriangulation.Intersections;
                    //_pslg = _conformingTriangulation.Pslg;
                    _vertices = _pslg.Vertices;
                }
            }
            else if (_addMode == AddMode.ConstrainedEdge)
            {
                if (_v0 != null)
                {
                    var p = SelectPoint(e.Location) ?? new Vertex(PointToWorld(e.Location));

                    if (_v0 != p)
                    {
                        _tempEdge = new Edge(_v0, p);
                        _pslg.AddEdge(_tempEdge);
                        //_intersections = BentleyOttmann.Intersect(_pslg.Edges, TOLERANCE);
                        _pslg.RemoveEdge(_tempEdge);
                        _testIntersections.Clear();
                        Edge.SegmentIntersect(_testEdge, _tempEdge, TOLERANCE, _testIntersections);
                    }
                }
            }

            _lastMousePosition = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_addMode == AddMode.Vertex)
                {
                    var p = new Vertex(PointToWorld(e.Location));

                    _vertices.Add(p);
                }
                else if (_addMode == AddMode.ConstrainedEdge)
                {
                    if (_tempEdge != null)
                        _pslg.AddEdge(_tempEdge);
                }

                if (_triangulationMode == TriangulationMode.Delaunay)
                {
                    if (_vertices.Count > 2)
                    {
                        foreach (var vertex in _vertices)
                            vertex.Edges.Clear();

                        _delaunayTriangulation = DelaunayTriangulation.Triangulate(_vertices);
                        _vertices = _vertices.Select(v => new Vertex(v.Position)).ToList();
                    }
                }
                else if (_triangulationMode == TriangulationMode.ConformingDelaunay)
                {
                    _conformingTriangulation = ConformingDelaunayTriangulation.Triangulate(_pslg, _vertices, TOLERANCE);

                    _testIntersections.Clear();
                    // _testIntersections.AddRange(Edge.Intersect2(_testEdge, _tempEdge, TOLERANCE));
                    _pslg = _conformingTriangulation.Pslg;
                    _vertices = _pslg.Vertices;

                    foreach (var vertex in _vertices)
                        vertex.Edges.Clear();
                }
            }

            _viewMode = ViewMode.None;

            _movingPointIndex = -1;

            _v0 = null;
            _tempEdge = null;
            this.Cursor = Cursors.Default;
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
                _zoomInverse = 1.0f / _zoom;
                _origin -= (p * _zoom);
            }
        }

        private Vec2 PointToWorld(Point screenCoords)
        {
            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;
            var p = new Vec2(screenCoords.X - halfWidth, -(screenCoords.Y - halfHeight));

            p += _origin;
            p *= _zoomInverse;
            return p;
        }

        private Vertex SelectPoint(Point screenCoords)
        {
            var position = PointToWorld(screenCoords);

            var minDist = 5 * _zoomInverse;
            for (int i = 0; i < _vertices.Count; i++)
            {
                var vertex = _vertices[i];

                if (Math.Abs(position.X - vertex.Position.X) < minDist && Math.Abs(position.Y - vertex.Position.Y) < minDist)
                    return _vertices[i];
            }

            return null;
        }

        private void UpdatePoints()
        {
            _timer.Stop();

            //_vertices.AddRange(FillPoints(10000, new RectangleF(-1000,-1000, 2000, 2000)));

            // rand

            //var halfWidth = ClientRectangle.Width * 0.5f * _zoom;
            //var halfHeight = ClientRectangle.Height * 0.5f * _zoom;

            //var bounds = new RectangleF(
            //    new PointF(-halfWidth, -halfHeight),
            //    new SizeF(ClientRectangle.Width, ClientRectangle.Height));

            //for (int i = 0; i < 1; i++)
            //{
            //    _points = FillPoints(_totalPoints, bounds);
            //    _triangles = DelaunayTriangulation.Triangulate(_points);
            //}

            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(880.844910777747, 1000)), new Vertex(new Vec2(853.570983886719, 1000))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1000, 500)), new Vertex(new Vec2(1000, 821.26737343927))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1000, 290.910827636719)), new Vertex(new Vec2(1000, 500))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1353.57104492188, 290.910827636719)), new Vertex(new Vec2(853.570983886719, 1040.91088867188))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(853.570983886719, 1040.91088867188)), new Vertex(new Vec2(853.570983886719, 290.910827636719))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(853.570983886719, 290.910827636719)), new Vertex(new Vec2(1353.57104492188, 290.910827636719))));

            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(2488.81787109375, 1113.5077583591)), new Vertex(new Vec2(2488.81787109375, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(2363.49889793366, 1115.24652929936)), new Vertex(new Vec2(2363.76858208171, 1111.54013426594))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1969.04443359375, 1105.32923334156)), new Vertex(new Vec2(1969.04443359375, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1869.42443847656, 1115.24652929936)), new Vertex(new Vec2(1869.42443847656, 1103.76173380517))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1635.11669921875, 1100.07495112435)), new Vertex(new Vec2(1635.11669921875, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1535.49658203125, 1115.24652929936)), new Vertex(new Vec2(1535.49658203125, 1098.5074496672))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1344.23388671875, 1095.49797164144)), new Vertex(new Vec2(1344.23388671875, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1245.75390625, 1115.24652929936)), new Vertex(new Vec2(1245.75390625, 1093.94840999422))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1117.72436523438, 1094.59108010523)), new Vertex(new Vec2(1117.72436523438, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(1117.72436523438, 1091.93389226662)), new Vertex(new Vec2(1117.72436523438, 1094.59108010523))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(983.264404296875, 1115.24652929936)), new Vertex(new Vec2(983.264404296875, 1089.81819324269))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(837.556284440744, 1087.52550681909)), new Vertex(new Vec2(835.539721799644, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(344.760589346842, 1103.86763674669)), new Vertex(new Vec2(341.220656622349, 1079.71577077577))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(346.428390673836, 1115.24652929936)), new Vertex(new Vec2(344.760589346842, 1103.86763674669))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(-1149.67114257813, 1056.25690389552)), new Vertex(new Vec2(2599.32275390625, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(2599.32275390625, 1115.24652929936)), new Vertex(new Vec2(-1149.67114257813, 1115.24652929936))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(-1149.67114257813, 1115.24652929936)), new Vertex(new Vec2(-1149.67114257813, 1056.25690389552))));


            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(100, 0)), new Vertex(new Vec2(600, 0))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(-200, 100)), new Vertex(new Vec2(300, 100))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(-100, 200)), new Vertex(new Vec2(400, 200))));
            //_pslg.AddEdge(new Edge(new Vertex(new Vec2(0, -200)), new Vertex(new Vec2(500, 500))));

            //var c = 2000;
            //var w = 100000;
            //var h = 100000;

            //for (int i = 0; i < c; i++)
            //{
            //    var x0 = (_rand.NextDouble() * 2.0 - 1.0) * w * 0.5;
            //    var y0 = (_rand.NextDouble() * 2.0 - 1.0) * h * 0.5;
            //    var x1 = (_rand.NextDouble() * 2.0 - 1.0) * w * 0.5;
            //    var y1 = (_rand.NextDouble() * 2.0 - 1.0) * h * 0.5;
            //    _pslg.AddEdge(new Edge(new Vertex(new Vec2(x0, y0)), new Vertex(new Vec2(x1, y1))));
            //}


            foreach (var edge in _pslg.Edges)
            {
                if (!_vertices.Contains(edge.V0))
                    _vertices.Add(edge.V0);

                if (!_vertices.Contains(edge.V1))
                    _vertices.Add(edge.V1);
            }

            _timer.Start();
        }

        private List<Vertex> FillPoints(int pointCount, RectangleF bounds)
        {
            var vertices = new List<Vertex>();

            for (int i = 0; i < pointCount; i++)
            {
                var x = (_rand.NextDouble() * 2.0 - 1.0) * bounds.Width * 0.5;
                var y = (_rand.NextDouble() * 2.0 - 1.0) * bounds.Height * 0.5;
                vertices.Add(new Vertex(new Vec2(Math.Round(x), Math.Round(y))));
            }

            return vertices;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var halfWidth = ClientRectangle.Width * 0.5f;
            var halfHeight = ClientRectangle.Height * 0.5f;

            InitDrawing(g, halfWidth, halfHeight);
            DrawAxis(g, halfWidth, halfHeight);

            DrawTriangulation(g);

            DrawPoints(g, _vertices.Select(v => v.Position).ToList(), Color.Blue);
            var selected = _selectedIndices.Select(p => _vertices.ElementAt(p).Position).ToList();

            if (selected.Count > 0)
                DrawPoints(g, selected, Color.Red);

            //using (var pen = new Pen(new SolidBrush(Color.BlueViolet), -1.0f))
            //{
            //    Draw(g, _testEdge, pen);

            //    using (var font = new Font(this.Font.FontFamily, 8.0f * _zoomInverse))
            //    {
            //        foreach (var testIntersection in _testIntersections)
            //        {
            //            if (testIntersection.Intersects)
            //            {
            //                Draw(g, testIntersection.Vertex.Position, Brushes.Red, font);
            //            }
            //        }
            //    }
            //}

            if (_drawOptions.DrawPSLG)
                DrawPSLG(g);

            if (_addMode == AddMode.ConstrainedEdge && _tempEdge != null)
            {
                using (var pen = new Pen(new SolidBrush(Color.Gray), -1.0f))
                {
                    pen.DashStyle = DashStyle.DashDot;
                    Draw(g, _tempEdge, pen);
                }
            }

            if (_intersections.Count > 0)
            {
                using (var font = new Font(this.Font.FontFamily, 8.0f * _zoomInverse))
                {
                    foreach (var item in _intersections)
                    {
                        Draw(g, item, Brushes.Green, font);
                    }
                }
            }
        }

        private void DrawPSLG(Graphics g)
        {
            if (_pslg != null)
            {
                foreach (var edge in _pslg.Edges)
                    Draw(g, edge, Color.Red, 1.0f * _zoomInverse);
            }
        }

        private void DrawTriangulation(Graphics g)
        {
            if (_conformingTriangulation != null)
                DrawConformingTriangulation(g, _conformingTriangulation);
            else if (_delaunayTriangulation != null)
                DrawDelaunayTriangulation(g, _delaunayTriangulation);
        }

        private void DrawDelaunayTriangulation(Graphics g, DelaunayTriangulation triangulation)
        {
            var triangles = triangulation.Triangles;

            if (triangles != null)
                DrawTriangles(g, triangles);
        }

        private void DrawConformingTriangulation(Graphics g, ConformingDelaunayTriangulation triangulation)
        {
            var triangles = triangulation.Triangles;

            if (triangles != null)
                DrawTriangles(g, triangles);
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

            g.Clear(this.BackColor);
        }

        private void DrawAxis(Graphics g, float halfWidth, float halfHeight)
        {
            var w0 = (-halfWidth + (float)_origin.X) * (_zoomInverse);
            var w1 = (halfWidth + (float)_origin.X) * (_zoomInverse);
            var h0 = (-halfHeight + (float)_origin.Y) * (_zoomInverse);
            var h1 = (halfHeight + (float)_origin.Y) * (_zoomInverse);

            g.SmoothingMode = SmoothingMode.Default;
            using (var pen = new Pen(Brushes.Black, -1))
            {
                g.DrawLine(pen, new PointF(w0, 0.0f), new PointF(w1, 0.0f));
                g.DrawLine(pen, new PointF(0.0f, h0), new PointF(0.0f, h1));
            }

            //var gridSize = 10.0f;

            //using (var pen = new Pen(Brushes.Gray, -1))
            //{
            //    pen.DashStyle = DashStyle.Dot;
            //    var x = 0.0f;
            //    do
            //    {
            //        x += gridSize;
            //        g.DrawLine(pen, new PointF(x, h0), new PointF(x, h1));
            //    } while (x < w1);

            //    x = 0.0f;
            //    do
            //    {
            //        x -= gridSize;
            //        g.DrawLine(pen, new PointF(x, h0), new PointF(x, h1));
            //    } while (x > w0);

            //    var y = 0.0f;
            //    do
            //    {
            //        y += gridSize;
            //        g.DrawLine(pen, new PointF(w0, y), new PointF(w1, y));
            //    } while (y < h1);

            //    y = 0.0f;
            //    do
            //    {
            //        y -= gridSize;
            //        g.DrawLine(pen, new PointF(w0, y), new PointF(w1, y));
            //    } while (y > h0);
            //}

            g.SmoothingMode = SmoothingMode.HighQuality;
        }

        private void DrawPoints(Graphics g, List<Vec2> points, Color color)
        {
            using (var font = new Font(this.Font.FontFamily, 8.0f * _zoomInverse))
            {
                using (var brush = new SolidBrush(color))
                {
                    using (var pen = new Pen(color, -1))
                    {
                        foreach (var point in points)
                        {
                            Draw(g, point, brush, font);

                            var rect = new RectangleF(
                                (float)point.X - (float)TOLERANCE,
                                (float)point.Y - (float)TOLERANCE,
                                2.0f * (float)TOLERANCE,
                                2.0f * (float)TOLERANCE);

                            g.DrawEllipse(pen, rect);
                        }
                    }
                }
            }
        }

        private void DrawTriangles(Graphics g, List<Triangle> triangles)
        {
            foreach (var triangle in triangles)
                Draw(g, triangle, Color.Green);
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

        private void Draw(Graphics g, Vec2 point, Brush brush, Font font)
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

            g.DrawString(string.Format("({0}, {1})", point.X, point.Y), font, Brushes.White, new PointF(rect.X + 6 * _zoomInverse, -rect.Y - 10 * _zoomInverse));

            g.Restore(state);
        }

        private void Draw(Graphics g, Triangle triangle, Color color)
        {
            if (triangle.E0 != null)
                Draw(g, triangle.E0, _drawOptions.DrawFlippableEdges && !triangle.E0.CanFlip() ? Color.Red : color);

            if (triangle.E1 != null)
                Draw(g, triangle.E1, _drawOptions.DrawFlippableEdges && !triangle.E1.CanFlip() ? Color.Red : color);

            if (triangle.E2 != null)
                Draw(g, triangle.E2, _drawOptions.DrawFlippableEdges && !triangle.E2.CanFlip() ? Color.Red : color);
        }

        private void Draw(Graphics g, Edge edge, Pen pen)
        {
            var p0 = edge.V0.Position.ToPointF();
            var p1 = edge.V1.Position.ToPointF();
            g.DrawLine(pen, p0.X, p0.Y, p1.X, p1.Y);

            var perp0 = TOLERANCE * Vec2.Perp(Vec2.Normalize(edge.V1.Position - edge.V0.Position));
            var perp1 = TOLERANCE * -Vec2.Perp(Vec2.Normalize(edge.V1.Position - edge.V0.Position));

            var v0 = (edge.V0.Position + perp0).ToPointF();
            var v1 = (edge.V1.Position + perp0).ToPointF();
            var v2 = (edge.V0.Position + perp1).ToPointF();
            var v3 = (edge.V1.Position + perp1).ToPointF();

            using (var bluePen = new Pen(Brushes.Blue, -1.0f))
            {
                g.DrawLine(pen, v0.X, v0.Y, v1.X, v1.Y);
                g.DrawLine(pen, v2.X, v2.Y, v3.X, v3.Y);

                var rect = new RectangleF(p0.X - (float)TOLERANCE, p0.Y - (float)TOLERANCE, 2.0f * (float)TOLERANCE, 2.0f * (float)TOLERANCE);

                g.DrawEllipse(pen, rect);

                rect = new RectangleF(p1.X - (float)TOLERANCE, p1.Y - (float)TOLERANCE, 2.0f * (float)TOLERANCE, 2.0f * (float)TOLERANCE);

                g.DrawEllipse(pen, rect);
            }
        }

        private void Draw(Graphics g, Edge edge, Color color, float width = -1.0f)
        {
            var pen = new Pen(new SolidBrush(color), width);
            Draw(g, edge, pen);
        }

        private void tsmSelect_Click(object sender, EventArgs e)
        {
            ChangeToSelectMode();
        }

        private void tsmMove_Click(object sender, EventArgs e)
        {
            ChangeToMoveMode();
        }

        private void tsmClearPoints_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            _timer.Stop();
            _vertices.Clear();
            _delaunayTriangulation = null;
            _conformingTriangulation = null;
            _pslg = null;
            _intersections.Clear();
            _timer.Start();
        }

        private void tsmAddPoints_Click(object sender, EventArgs e)
        {
            ChangeToAddVertexMode();
        }

        private void ChangeToAddVertexMode()
        {
            _editMode = EditMode.Add;
            _addMode = AddMode.Vertex;
        }

        private void tsmConstrainedEdge_Click(object sender, EventArgs e)
        {
            ChangeToAddConstrainedEdgeMode();
        }

        private void ChangeToAddConstrainedEdgeMode()
        {
            _editMode = EditMode.Add;
            _addMode = AddMode.ConstrainedEdge;

            _drawOptions.DrawPSLG = true;
            _triangulationMode = TriangulationMode.ConformingDelaunay;
        }

        private void ChangeToSelectMode()
        {
            _editMode = EditMode.Select;
            _selectedIndices.Clear();

            _addMode = AddMode.None;

            this.Cursor = Cursors.Default;
        }

        private void ChangeToMoveMode()
        {
            _editMode = EditMode.Move;

            _addMode = AddMode.None;

            this.Cursor = Cursors.SizeAll;
        }

        private void tsmShowPSLG_Click(object sender, EventArgs e)
        {
            _drawOptions.DrawPSLG = !_drawOptions.DrawPSLG;
        }

        private void tsmShowFlippableEdges_Click(object sender, EventArgs e)
        {
            _drawOptions.DrawFlippableEdges = !_drawOptions.DrawFlippableEdges;
        }

        private void tsmDelaunay_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            _triangulationMode = TriangulationMode.Delaunay;
            _delaunayTriangulation = DelaunayTriangulation.Triangulate(_vertices);
            _timer.Start();
        }

        private void tsmConformingDelaunay_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            _triangulationMode = TriangulationMode.ConformingDelaunay;
            _conformingTriangulation = ConformingDelaunayTriangulation.Triangulate(_pslg, _vertices, TOLERANCE);
            _pslg = _conformingTriangulation.Pslg;
            _vertices = _pslg.Vertices;
            _timer.Start();
        }

        List<Vec2> _intersections = new List<Vec2>();
        private void bentleyOttmannToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _intersections = BentleyOttmann.Intersect(_pslg.Edges, TOLERANCE);
            stopwatch.Stop();

            System.Windows.Forms.MessageBox.Show(stopwatch.ElapsedMilliseconds.ToString());
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
                this.Close();

            if (keyData == Keys.Delete)
                Clear();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void naiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var a in _pslg.Edges)
            {
                foreach (var b in _pslg.Edges)
                {
                    if (a == b)
                        continue;

                    var intersections = new List<EdgeIntersection>();
                    Edge.SegmentIntersect(a, b, TOLERANCE, intersections);
                    foreach (var i in intersections)
                    {
                        _intersections.Add(i.Vertex.Position);
                    }
                }
            }

            stopwatch.Stop();
            System.Windows.Forms.MessageBox.Show(stopwatch.ElapsedMilliseconds.ToString());
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

