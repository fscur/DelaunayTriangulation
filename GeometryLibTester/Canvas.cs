using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TriangleLib;

namespace GeometryLibTester
{
    public class Canvas : System.Windows.Forms.Panel
    {
        Dictionary<long, IDrawable> _drawables;

        float _halfWidth = 0.0f;
        float _halfHeight = 0.0f;
        float _zoom = 0.0f;
        float _zoomInverse = 0.0f;
        Vec2 _origin = Vec2.Zero;
        Point _lastMousePosition = Point.Empty;
        Timer _timer;
        bool _panning = false;
        SolidBrush _mainAxisBrush;
        private Color _mainAxisColor;

        public Color MainAxisColor
        {
            get { return _mainAxisColor; }
            set { _mainAxisColor = value; }
        }
        
        public Canvas()
        {
            _zoom = 1.0f;
            _zoomInverse = 1.0f / _zoom;

            _mainAxisColor = Color.FromArgb(255, 83, 83, 83);
            _mainAxisBrush = new SolidBrush(_mainAxisColor);

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            _halfWidth = this.Bounds.Width * 0.5f;
            _halfHeight = this.Bounds.Height * 0.5f;
            base.OnLayout(levent);
            this.Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            this.Invalidate();
            base.OnMouseWheel(e);

            var p = new Vec2(e.Location.X - _halfWidth, -(e.Location.Y - _halfHeight));
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:

                    this.Cursor = Cursors.SizeAll;
                    _panning = true;
                    _lastMousePosition = e.Location;
                    break;
                default:
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    if (_panning)
                    {
                        this.Cursor = Cursors.SizeAll;
                        var dx = _lastMousePosition.X - e.Location.X;
                        var dy = -(_lastMousePosition.Y - e.Location.Y);
                        _origin += new Vec2(dx, dy);
                    }
                    break;
                default:
                    break;
            }

            _lastMousePosition = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
                default:
                    break;
            }

            _panning = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down) return true;
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnEnter(EventArgs e)
        {
            this.Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            this.Invalidate();
            base.OnLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!DesignMode && _timer == null)
            {
                _timer = new Timer();
                _timer.Interval = 10;
                _timer.Tick += (sender, ev) => { this.Invalidate(); };
                _timer.Start();
            }

            var g = e.Graphics;

            InitCanvas(g);

            DrawAxis(g);

            g.FillEllipse(Brushes.Red, new RectangleF(100, 100, 100, 100));
        }

        private Vec2 PointToWorld(Point screenCoords)
        {
            var p = new Vec2(screenCoords.X - _halfWidth, -(screenCoords.Y - _halfHeight));

            p += _origin;
            p *= _zoomInverse;

            return p;
        }

        private void InitCanvas(Graphics g)
        {
            var clientToWorld = new Matrix(
                            1.0f, 0.0f,
                            0.0f, -1.0f,
                            _halfWidth, _halfHeight);


            var translate = new Matrix(
                            1.0f, 0.0f,
                            0.0f, 1.0f,
                            -(float)_origin.X, -(float)_origin.Y);

            var scale = new Matrix(
                            _zoom, 0.0f,
                            0.0f, _zoom,
                            0.0f, 0.0f);

            g.MultiplyTransform(clientToWorld);
            g.MultiplyTransform(translate);
            g.MultiplyTransform(scale);


            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(this.BackColor);
        }

        private void DrawAxis(Graphics g)
        {
            var w0 = (-_halfWidth + (float)_origin.X) * (_zoomInverse);
            var w1 = (_halfWidth + (float)_origin.X) * (_zoomInverse);
            var h0 = (-_halfHeight + (float)_origin.Y) * (_zoomInverse);
            var h1 = (_halfHeight + (float)_origin.Y) * (_zoomInverse);

            g.SmoothingMode = SmoothingMode.Default;
            
            using (var pen = new Pen(_mainAxisBrush, -1))
            {
                g.DrawLine(pen, new PointF(w0, 0.0f), new PointF(w1, 0.0f));
                g.DrawLine(pen, new PointF(0.0f, h0), new PointF(0.0f, h1));
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
        }
        
        public void DrawLine(Vec2 start, Vec2 end, Pen pen)
        {
            var id = GetNewID(DrawableType.Line);
            _drawables.Add()
        }

        static long GetNewId(DrawableType type)
        {

        }
    }

    public interface IDrawable
    {
        long ID { get; set; }
        DrawableType Type { get; set; }
        Material Material { get; set; }
    }

    public struct Material
    {
        public Color Color { get; set; }
    }

    public enum DrawableType
    {
        Line
    }
}