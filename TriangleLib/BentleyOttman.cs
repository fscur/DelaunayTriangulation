using C5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TriangleLib.Edge;

namespace TriangleLib
{
    public enum EventType
    {
        START,
        END,
        INTERSECTION
    }

    public class Event : IComparable<Event>
    {
        public EventType EventType;
        public Vec2 Position;
        public Edge Segment;
        protected SweepLine SweepLine;
        public Event(EventType eventType, Vec2 position, Edge segment, SweepLine sweepLine)
        {
            EventType = eventType;
            Position = position;
            Segment = segment;
            SweepLine = sweepLine;
        }
        
        public int CompareTo(Event that)
        {
            if (this == that || this.Equals(that))
                return 0;

            var ipThis = SweepLine.Intersection(this);
            var ipThat = SweepLine.Intersection(that);

            var deltaY = ipThis.Y - ipThat.Y;

            var almostZero = Compare.Greater(deltaY, -0.5E-6) && Compare.Less(deltaY, 0.5E-6);

            if (!almostZero)
                return Compare.Less(deltaY, 0) ? -1 : 1;
            else
            {
                var thisSlope = this.Segment.Slope;
                var thatSlope = that.Segment.Slope;
                
                if (!Compare.AlmostEqual(thisSlope, thatSlope))
                {
                    if (SweepLine.IsBefore())
                        return Compare.Greater(thisSlope, thatSlope) ? -1 : 1;
                    else
                        return Compare.Greater(thisSlope, thatSlope) ? 1 : -1;
                }

                var deltaXP1 = this.Segment.V0.Position.X - that.Segment.V0.Position.X;

                if (!Compare.AlmostEqual(deltaXP1, 0.0))
                    return Compare.Less(deltaXP1, 0.0) ? -1 : 1;

                var deltaXP2 = this.Segment.V1.Position.X - that.Segment.V1.Position.X;

                return Compare.Less(deltaXP2, 0.0) ? -1 : 1;
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            if (obj == null || GetType() != obj.GetType())
                return false;

            Event that = (Event)obj;

            if ((this.EventType == EventType.INTERSECTION && that.EventType != EventType.INTERSECTION) ||
                (this.EventType != EventType.INTERSECTION && that.EventType == EventType.INTERSECTION))
                return false;
            else if (this.EventType == EventType.INTERSECTION && that.EventType == EventType.INTERSECTION)
                return this.Position.Equals(that.Position);
            else
                return this.Segment.Equals(that.Segment);
        }
        
        public override int GetHashCode()
        {
            return this.EventType == EventType.INTERSECTION ? Position.GetHashCode() : Segment.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", EventType, Position, Segment);
        }
    }
    
    public class EventQueue
    {
        private TreeDictionary<Vec2, List<Event>> _events;
        
        public EventQueue(List<Edge> segments, SweepLine sweepLine)
        {
            if (segments.Count == 0)
                throw new ArgumentException("'segments' cannot be empty.");

            _events = new TreeDictionary<Vec2, List<Event>>(new Vec2Comparer());

            Init(segments, sweepLine);
        }
        
        private void Init(List<Edge> segments, SweepLine sweepLine)
        {
            var minY = double.MaxValue;
            var maxY = double.MinValue;
            var minDeltaX = double.MaxValue;
            TreeSet<double> xs = new TreeSet<double>();

            foreach (Edge s in segments)
            {
                xs.Add(s.V0.Position.X);
                xs.Add(s.V1.Position.X);

                if (Compare.Less(s.V0.Position.Y, minY))
                    minY = s.V0.Position.Y;

                if (Compare.Less(s.V1.Position.Y, minY))
                    minY = s.V1.Position.Y;

                if (Compare.Greater(s.V0.Position.Y, maxY))
                    maxY = s.V0.Position.Y;

                if (Compare.Greater(s.V1.Position.Y, maxY))
                    maxY = s.V1.Position.Y;

                Enqueue(s.V0.Position, new Event(EventType.START, s.V0.Position, s, sweepLine));
                Enqueue(s.V1.Position, new Event(EventType.END, s.V1.Position, s, sweepLine));
            }

            var xsArray = xs.ToArray();
            for (int i = 1; i < xsArray.Length; i++)
            {
                var tempDeltaX = xsArray[i] - (xsArray[i - 1]);
                if (Compare.Less(tempDeltaX, minDeltaX))
                {
                    minDeltaX = tempDeltaX;
                }
            }

            var deltaY = maxY - minY;
            var slope = -(deltaY / minDeltaX);

            var x = 1.0;
            var y = slope * x;
            
            sweepLine.SetQueue(this);
        }
        
        public bool IsEmpty()
        {
            return _events.Count == 0;
        }
        
        public void Enqueue(Vec2 p, Event e)
        {
            List<Event> existing;

            if (_events.Contains(p))
            {
                existing = _events[p];
                _events.Remove(p);
            }
            else
                existing = new List<Event>();

            // END events should be at the start of the list
            if (e.EventType == EventType.END)
            {
                if (!existing.Contains(e))
                    existing.Insert(0, e);
            }
            else
            {
                if (!existing.Contains(e))
                    existing.Add(e);
            }

            _events.Add(p, existing);
        }
        
        public List<Event> Dequeue()
        {
            if (this.IsEmpty())
                throw new Exception("empty");

            var entry = _events.DeleteMin();

            return new List<Event>(entry.Value);
        }
    }

    public class SweepLine
    {
        private TreeSet<Event> _events;
        private Dictionary<Vec2, List<Event>> _intersections;
        private double _position;
        private Vec2 _currentEventPoint;
        private bool _before;
        private EventQueue _queue;

        public void SetQueue(EventQueue queue)
        {
            _queue = queue;
        }

        public SweepLine()
        {
            _events = new TreeSet<Event>();
            _intersections = new Dictionary<Vec2, List<Event>>();
            _position = double.MinValue;
            _currentEventPoint = null;
            _before = true;
        }
        
        public Event GetSegmentAbove(Event e)
        {
            Event sucessor;
            if (_events.TrySuccessor(e, out sucessor))
                return sucessor;
            return null;
        }

        public Event GetSegmentBelow(Event e)
        {
            Event predecessor;
            if (_events.TryPredecessor(e, out predecessor))
                return predecessor;

            return null;
        }

        private void CheckIntersection(Event a, Event b)
        {
            // Return immediately in case either of the events is null, or
            // if one of them is an INTERSECTION event.
            if (a == null || b == null || a.EventType == EventType.INTERSECTION ||
                    b.EventType == EventType.INTERSECTION)
            {
                return;
            }

            // Get the intersection point between 'a' and 'b'.
            EdgeIntersection intersection;
            if (!Intersect(a.Segment, b.Segment, out intersection)) return;

            var p = intersection.Vertex.Position;

            if (Compare.Less(p.X, _position))
                return;
            // Add the intersection.

            List<Event> existing;

            if (_intersections.ContainsKey(p))
            {
                existing = _intersections[p];
                _intersections.Remove(p);
            }
            else
                existing = new List<Event>();

            if (!existing.Contains(a))
                existing.Add(a);
            if (!existing.Contains(b))
                existing.Add(b);

            _intersections.Add(p, existing);

            // If the intersection occurs to the right of the sweep line, OR
            // if the intersection is on the sweep line and it's above the
            // current event-point, add it as a new Event to the queue.
            if (Compare.Greater(p.X, _position) || (Compare.AlmostEqual(p.X, _position) && Compare.Greater(p.Y, _currentEventPoint.Y)))
            {
                Event intersectionEvent = new Event(EventType.INTERSECTION, p, null, this);

                _queue.Enqueue(p, intersectionEvent);
            }
        }

        public bool Intersect(Edge a, Edge b, out EdgeIntersection intersection)
        {
            intersection = Edge.Intersect(a, b);

            return (intersection.Intersects &&
                Compare.Greater(intersection.S, 0.5e-6, Compare.TOLERANCE) &&
                Compare.Less(intersection.S, 1.0 - 0.5e-6, Compare.TOLERANCE) &&
                Compare.Greater(intersection.T, 0.5e-6, Compare.TOLERANCE) &&
                Compare.Less(intersection.T, 1.0 - 0.5e-6, Compare.TOLERANCE));
        }

        public Dictionary<Vec2, List<Edge>> GetIntersections()
        {
            var segments = new Dictionary<Vec2, List<Edge>>();
            foreach (var entry in this._intersections)
            {
                var set = new List<Edge>();
                foreach (var e in entry.Value)
                {
                    set.Add(e.Segment);
                }

                segments.Add(entry.Key, set);
            }
            return segments;
        }
        
        public void HandleEvents(List<Event> events)
        {
            if (events.Count == 0)
                return;

            var array = events.ToArray();
            SweepTo(array[0]);
            
            foreach (var e in events)
            {
                HandleEvent(e);
            }
        }
        
        private void HandleEvent(Event e)
        {
            switch (e.EventType)
            {
                case EventType.START:
                    _before = false;
                    Insert(e);
                    var sabove = GetSegmentAbove(e);
                    var sbelow = GetSegmentBelow(e);
                    CheckIntersection(e, sabove);
                    CheckIntersection(e, sbelow);
                    break;
                case EventType.END:
                    _before = true;
                    Remove(e);
                    CheckIntersection(GetSegmentAbove(e), GetSegmentBelow(e));
                    break;
                case EventType.INTERSECTION:
                    _before = true;
                    var set = _intersections[e.Position];
                    var toInsert = new Stack<Event>();

                    foreach (var ev in set)
                    {
                        // If we the Event was not already removed, we want to insert it later on.
                        if (Remove(ev))
                        {
                            toInsert.Push(ev);
                        }
                    }

                    _before = false;
                    // Insert all Events that we were able to remove.
                    while (toInsert.Count > 0)
                    {
                        var ev = toInsert.Pop();
                        Insert(ev);
                        sabove = GetSegmentAbove(ev);
                        sbelow = GetSegmentBelow(ev);
                        CheckIntersection(ev, sabove);
                        CheckIntersection(ev, sbelow);
                    }
                    break;
            }
        }
        
        public bool Insert(Event e)
        {
            return _events.Add(e);
        }
        
        public Vec2 Intersection(Event e)
        {
            if (e.EventType == EventType.INTERSECTION)
            {
                return e.Position;
            }
            else
            {
                var x0 = e.Segment.V0.Position.X;
                var y0 = e.Segment.V0.Position.Y;
                var x1 = e.Segment.V1.Position.X;
                var y1 = e.Segment.V1.Position.Y;
                var m = (y1 - y0) / (x1 - x0);
                var b = y1 - m * x1;

                return new Vec2(this._position, m * this._position + b);
            }
        }
        
        public bool IsBefore()
        {
            return _before;
        }
        
        public bool Remove(Event e)
        {
            return _events.Remove(e);
        }
        
        private void SweepTo(Event e)
        {
            _currentEventPoint = e.Position;
            _position = e.Position.X;
        }
    }

    public class BentleyOttmann
    {
        private BentleyOttmann()
        {
        }

        public static List<Vec2> Intersect(List<Edge> segments)
        {
            return IntersectFull(segments).Keys.ToList();
        }

        public static Dictionary<Vec2, List<Edge>> IntersectFull(List<Edge> segments)
        {
            if (segments.Count < 2)
            {
                return new Dictionary<Vec2, List<Edge>>();
            }

            SweepLine sweepLine = new SweepLine();
            EventQueue queue = new EventQueue(segments, sweepLine);

            while (!queue.IsEmpty())
            {
                var events = queue.Dequeue();
                sweepLine.HandleEvents(events);
            }

            return sweepLine.GetIntersections();
        }
    }
}
