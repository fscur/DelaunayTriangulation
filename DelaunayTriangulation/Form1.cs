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
        static readonly float POINT_SIZE = 10f;
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
            else if (_selectPoints && e.Button == MouseButtons.Left)
            {
                var halfWidth = ClientRectangle.Width * 0.5f;
                var halfHeight = ClientRectangle.Height * 0.5f;
                var p = new Vec2(e.Location.X - halfWidth, -(e.Location.Y - halfHeight));

                p += _origin;
                p *= 1.0 / _zoom;

                Vec2 pToRemove = null;

                foreach (var point in _points)
                {
                    if (Math.Abs(p.X - point.X) < 5.0 && Math.Abs(p.Y - point.Y) < 5.0)
                    {
                        pToRemove = point;
                        _points.Add(new Vec2(Math.Round(p.X), Math.Round(p.Y)));
                        break;
                    }
                }

                _points.Remove(pToRemove);
                if (_points.Count > 2)
                    _triangles = DelaunayTriangulation.Triangulate(_points);
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
                p *= 1.0 / _zoom;

                _points.Add(new Vec2(Math.Round(p.X), Math.Round(p.Y)));
                if (_points.Count > 2)
                    _triangles = DelaunayTriangulation.Triangulate(_points);
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
                _zoomInverse = 1.0f / _zoom;
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

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(839.5, 541.6)), new Vertex(new Vec2(839.5, 542.8))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(839.5, 542.8)), new Vertex(new Vec2(857.5, 925.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(837.4, 539.7)), new Vertex(new Vec2(839.5, 541.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(645.6, 354.4)), new Vertex(new Vec2(837.4, 539.7))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(800.8, 784.5)), new Vertex(new Vec2(559.7, 440.3))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(802.7, 787.3)), new Vertex(new Vec2(800.8, 784.5))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(799.7, 786.7)), new Vertex(new Vec2(802.7, 787.3))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(301.2, 698.8)), new Vertex(new Vec2(799.7, 786.7))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(484.5, 925.6)), new Vertex(new Vec2(208.1, 791.9))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(485.7, 925.6)), new Vertex(new Vec2(484.5, 925.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(857.5, 925.6)), new Vertex(new Vec2(485.7, 925.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 1000)), new Vertex(new Vec2(1000, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 0)), new Vertex(new Vec2(1000, 1000))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 1000)), new Vertex(new Vec2(0, 1000))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(769.237587695873, 230.762412304127)), new Vertex(new Vec2(1000, 453.518323197259))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 733.396673982009)), new Vertex(new Vec2(697.95931504673, 302.04068495327))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(999.715538875667, 736.74697949247)), new Vertex(new Vec2(1000, 736.797138018277))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(373.646639705853, 626.353360294148)), new Vertex(new Vec2(999.715538875667, 736.74697949247))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(684.545043945313, 875.634155273438)), new Vertex(new Vec2(307.049604096066, 692.950395903934))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(685.718148368993, 875.634155273438)), new Vertex(new Vec2(684.545043945313, 875.634155273438))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 875.634155273438)), new Vertex(new Vec2(685.718148368993, 875.634155273438))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 1000)), new Vertex(new Vec2(1000, 6.12303176911189E-14))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 6.12303176911189E-14)), new Vertex(new Vec2(1000, 1000))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 1000)), new Vertex(new Vec2(0, 1000))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(743.8, 256.2)), new Vertex(new Vec2(1000, 503.5))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 783.4)), new Vertex(new Vec2(677.4, 322.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(331.1, 668.9)), new Vertex(new Vec2(1000, 786.8))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(544.5, 857.9)), new Vertex(new Vec2(273.4, 726.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(684.5, 925.6)), new Vertex(new Vec2(544.5, 857.9))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(974.9, 925.6)), new Vertex(new Vec2(684.5, 925.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 925.6)), new Vertex(new Vec2(974.9, 925.6))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 1000)), new Vertex(new Vec2(1000, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 0)), new Vertex(new Vec2(1000, 1000))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 1000)), new Vertex(new Vec2(0, 1000))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0,0)), new Vertex(new Vec2(200,0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(200, 0)), new Vertex(new Vec2(200, 200))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(200, 200)), new Vertex(new Vec2(0, 200))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 200)), new Vertex(new Vec2(0, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 200)), new Vertex(new Vec2(200, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 200)), new Vertex(new Vec2(100, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 200)), new Vertex(new Vec2(200, 100))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(28.2, 386.4)), new Vertex(new Vec2(13.6, 386.4   ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(13.6, -46.4)), new Vertex(new Vec2(93.9, -46.4   ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(93.9, -46.4)), new Vertex(new Vec2(102.5, -46.4  ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(117.2, -131.8)), new Vertex(new Vec2(13.6, 471.7 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(13.6, 471.7)), new Vertex(new Vec2(13.6, -131.8  ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(13.6, -131.8)), new Vertex(new Vec2(117.2, -131.8))));


            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2224.27956003284, -480.767924713218)), new Vertex(new Vec2(2225.419921875, -467.733734130859   ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2042.38989257813, -2559.74365234375)), new Vertex(new Vec2(2224.27956003284, -480.767924713218 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2038.55298676632, -2555.17092899296)), new Vertex(new Vec2(2042.38989257813, -2559.74365234375 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1426.55993652344, -1825.81372070313)), new Vertex(new Vec2(2038.55298676632, -2555.17092899296 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1420.11464718629, -1839.63575360959)), new Vertex(new Vec2(1426.55993652344, -1825.81372070313 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1200.97921789528, -2309.57541902938)), new Vertex(new Vec2(1420.11464718629, -1839.63575360959 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1328.69995117188, -548.743713378906)), new Vertex(new Vec2(973.898766295768, -1872.88224287648 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1287.73999023438, -520.063720703125)), new Vertex(new Vec2(1287.99518975922, -520.242410338954 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1287.99518975922, -520.242410338954)), new Vertex(new Vec2(1328.69995117188, -548.743713378906 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2219.57774166932, -468.059774180774)), new Vertex(new Vec2(1287.73999023438, -520.063720703125 ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2225.419921875, -467.733734130859)), new Vertex(new Vec2(2219.57774166932, -468.059774180774   ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(0, 0)), new Vertex(new Vec2(2600, -5000                                                        ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2600, -5000)), new Vertex(new Vec2(2600, 1.59197958635171E-13))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(2600, 1.59197958635171E-13)), new Vertex(new Vec2(0, 0))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1818.09279008686, 390.94873046875)), new Vertex(new Vec2(-2185.52001953125, 390.94873046875         ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-2185.52001953125, 0)), new Vertex(new Vec2(1.22460635382238E-13, 2325.42993164063                   ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1.22460635382238E-13, 2325.42993164063)), new Vertex(new Vec2(-2185.52001953125, 2325.42993164063    ))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-2185.52001953125, 2325.42993164063)), new Vertex(new Vec2(-2185.52001953125, 0))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-221.750011261987, 152.903381347656)), new Vertex(new Vec2(-220.75439453125, 152.903381347656))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-767.5146484375, 152.903381347656)), new Vertex(new Vec2(-221.750011261987, 152.903381347656))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-767.5146484375, 153.306960726443)), new Vertex(new Vec2(-767.5146484375, 152.903381347656))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-767.5146484375, 153.653461995886)), new Vertex(new Vec2(-767.5146484375, 153.306960726443))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-220.75439453125, 152.903381347656)), new Vertex(new Vec2(-220.75439453125, 153.306960726443))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-220.75439453125, 153.306960726443)), new Vertex(new Vec2(-220.75439453125, 154.795664301721))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-220.75439453125, 154.795664301721)), new Vertex(new Vec2(-220.75439453125, 156.639308361081))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-220.75439453125, 156.639308361081)), new Vertex(new Vec2(-220.75439453125, 220.75439453125))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 296.60684204101)), new Vertex(new Vec2(-767.514648439987, 296.60684204101))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-849.75683593965, 849.75683593965)), new Vertex(new Vec2(-1000, 849.75683593901))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-2.44921270764475E-13, 5.99864284026878E-29)), new Vertex(new Vec2(-1000, 1000))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 1000)), new Vertex(new Vec2(-1000, 0))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 0)), new Vertex(new Vec2(-2.44921270764475E-13, 5.99864284026878E-29))));

            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(793.494323173917, 1000)), new Vertex(new Vec2(578.44873046875, 1000))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 250)), new Vertex(new Vec2(1000, 250))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1000, 250)), new Vertex(new Vec2(1000, 456.47705859375))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(1078.44873046875, 250)), new Vertex(new Vec2(578.44873046875, 1566))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(578.44873046875, 1566)), new Vertex(new Vec2(578.44873046875, 250))));
            //_pslg.Edges.Add(new Edge(new Vertex(new Vec2(578.44873046875, 250)), new Vertex(new Vec2(1078.44873046875, 250))));

            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-767.615158760077, 287.291017207003)), new Vertex(new Vec2(-737.083621152535, 405.125605552783))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-737.083621152535, 405.125605552783)), new Vertex(new Vec2(-701.307829207229, 543.200070416224))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-667.35300501428, 674.246613215096)), new Vertex(new Vec2(-592.259958401498, 964.063509334772 ))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-743.117666519443, 951.223988355546)), new Vertex(new Vec2(-861.546513658719, 941.144492339549))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-592.259958398943, 964.063509339778)), new Vertex(new Vec2(-661.956159545685, 958.131655752091))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-661.956159545685, 958.131655752091)), new Vertex(new Vec2(-743.117666519443, 951.223988355546))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-905.343768411527, 937.416900339186)), new Vertex(new Vec2(-982.887293405831, 930.817157385429))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-982.887293405831, 930.817157385429)), new Vertex(new Vec2(-1000, 929.360691938793            ))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-861.546513655736, 941.144492339803)), new Vertex(new Vec2(-905.343768411527, 937.416900339186))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-811.912487307497, 283.520863212556)), new Vertex(new Vec2(-767.615158754517, 287.291017212538))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-843.401244107527, 280.840849173737)), new Vertex(new Vec2(-811.912487307497, 283.520863212556))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 267.512699481502)), new Vertex(new Vec2(-843.401244107527, 280.840849173737             ))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-660.037935722971, 663.401576456208)), new Vertex(new Vec2(-660.041865036578, 663.407401903486))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-660.041865036578, 663.407401903486)), new Vertex(new Vec2(-667.353005014723, 674.246613212528))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-617.218444822462, 599.919067380208)), new Vertex(new Vec2(-658.035902378782, 660.433439813947))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-658.035902378782, 660.433439813947)), new Vertex(new Vec2(-660.037935722971, 663.401576456208))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-701.307829208746, 543.200070417168)), new Vertex(new Vec2(-617.218444825669, 599.919067381835))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 0)), new Vertex(new Vec2(6.12303176911189E-14, 1000                                    ))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(6.12303176911189E-14, 1000)), new Vertex(new Vec2(-1000, 1000                                 ))));
            _pslg.Edges.Add(new Edge(new Vertex(new Vec2(-1000, 1000)), new Vertex(new Vec2(-1000, 0))));

            foreach (var edge in _pslg.Edges)
            {
                if (!_points.Contains(edge.V0.Position))
                    _points.Add(Vec2.Round(edge.V0.Position));

                if (!_points.Contains(edge.V1.Position))
                    _points.Add(Vec2.Round(edge.V1.Position));
            }

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
                //DrawCircumcircle(g, t, Pens.Fuchsia);
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

                //if (Triangle.Contains(triangle, p))
                //    DrawCircumcircle(g, triangle, Pens.Fuchsia);
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
            using (var font = new Font(this.Font.FontFamily, 8.0f * _zoomInverse))
                g.DrawString(string.Format("({0}, {1})", point.X, point.Y), font, Brushes.Black, new PointF(rect.X + 6 * _zoomInverse, -rect.Y - 10 * _zoomInverse));

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

