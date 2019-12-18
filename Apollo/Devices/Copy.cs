using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Copy: Device {
        Random RNG = new Random();
        
        public List<Offset> Offsets;
        List<int> Angles;

        public void Insert(int index, Offset offset = null, int angle = 0) {
            Offsets.Insert(index, offset?? new Offset());
            Offsets.Last().Changed += OffsetChanged;

            Angles.Insert(index, angle);

            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Insert(index, Offsets[index], Angles[index]);
        }

        public void Remove(int index) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Offsets[index].Changed -= OffsetChanged;
            Offsets.RemoveAt(index);

            Angles.RemoveAt(index);
        }

        void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetOffset(Offsets.IndexOf(sender), sender);
        }

        public int GetAngle(int index) => Angles[index];
        public void SetAngle(int index, int angle) {
            if (-150 <= angle && angle <= 150 && angle != Angles[index]) {
                Angles[index] = angle;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetOffsetAngle(index, angle);
            }
        }

        Time _time;
        public Time Time {
            get => _time;
            set {
                if (_time != null) {
                    _time.FreeChanged -= FreeChanged;
                    _time.ModeChanged -= ModeChanged;
                    _time.StepChanged -= StepChanged;
                }

                _time = value;

                if (_time != null) {
                    _time.Minimum = 10;
                    _time.Maximum = 5000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;
                }
            }
        }

        void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateValue(value);
        }

        void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        class PolyInfo {
            public Signal n;
            public int index = 0;
            public object locker = new object();
            public List<int> offsets;
            public List<Courier> timers = new List<Courier>();
            
            public List<DoubleTuple> tupleOffsets;

            public PolyInfo(Signal init_n, List<int> init_offsets) {
                n = init_n;
                offsets = init_offsets;
            }
            public PolyInfo(Signal init_n, List<DoubleTuple> init_offsets) {
                n = init_n;
                tupleOffsets = init_offsets;
            }
        }

        CopyType _copymode;
        public CopyType CopyMode {
            get => _copymode;
            set {
                _copymode = value;
                
                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetCopyMode(CopyMode);

                Stop();
            }
        }

        GridType _gridmode;
        public GridType GridMode {
            get => _gridmode;
            set {
                _gridmode = value;
                
                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }
        
        double _pinch;
        public double Pinch {
            get => _pinch;
            set {
                if (-2 <= value && value <= 2) {
                    _pinch = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer)?.SetPinch(Pinch);
                }
            }
        }

        bool _reverse;
        public bool Reverse {
            get => _reverse;
            set {
                _reverse = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetReverse(Reverse);
            }
        }
        
        bool _infinite;
        public bool Infinite {
            get => _infinite;
            set {
                _infinite = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetInfinite(Infinite);
            }
        }
        
        double ActualPinch => (Pinch < 0)? ((1 / (1 - Pinch)) - 1) * .9 + 1 : 1 + (Pinch * 4 / 3);

        double ApplyPinch(double time, double total) => (1 - Math.Pow(1 - Math.Pow(Math.Min(1, Math.Max(0, time / total)), ActualPinch), 1 / ActualPinch)) * total;

        ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        ConcurrentDictionary<Signal, Courier> timers = new ConcurrentDictionary<Signal, Courier>();
        ConcurrentHashSet<PolyInfo> poly = new ConcurrentHashSet<PolyInfo>();

        public override Device Clone() => new Copy(_time.Clone(), _gate, Pinch, Reverse, Infinite, CopyMode, GridMode, Wrap, (from i in Offsets select i.Clone()).ToList(), Angles.ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Copy(Time time = null, double gate = 1, double pinch = 0, bool reverse = false, bool infinite = false, CopyType copymode = CopyType.Static, GridType gridmode = GridType.Full, bool wrap = false, List<Offset> offsets = null, List<int> angles = null): base("copy") {
            Time = time?? new Time(free: 500);
            Gate = gate;
            Pinch = pinch;

            Reverse = reverse;
            Infinite = infinite;

            CopyMode = copymode;
            GridMode = gridmode;
            Wrap = wrap;

            Offsets = offsets?? new List<Offset>();
            Angles = angles?? new List<int>();

            foreach (Offset offset in Offsets)
                offset.Changed += OffsetChanged;
        }

        void FireCourier(PolyInfo info, double time) {
            Courier courier;

            info.timers.Add(courier = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = time,
            });
            courier.Elapsed += Tick;
            courier.Start();
        }

        void FireCourier((Signal n, List<DoubleTuple>) info, double time) {
            Courier courier = timers[info.n.With(info.n.Coordinates, new Color())] = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;
            
            if ((CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) && courier.Info is PolyInfo info) {
                lock (info.locker) {
                    if (++info.index < info.tupleOffsets.Count) {
                        Signal m = info.n.Clone();
                        m.Coordinates = info.tupleOffsets[info.index];
                        InvokeExit(m);

                        if (info.index == info.tupleOffsets.Count - 1)
                            poly.Remove(info);
                    }
                }

            } else if (CopyMode == CopyType.RandomLoop && courier.Info is ValueTuple<Signal, List<int>>) {
                (Signal n, List<DoubleTuple> offsets) = ((Signal, List<DoubleTuple>))courier.Info;
                HandleRandomLoop(n, offsets);
            }
        }

        void HandleRandomLoop(Signal original, List<DoubleTuple> offsets) {
            Signal n = original.With(original.Coordinates, new Color());
            Signal m = original.Clone();

            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = RNG.Next(offsets.Count);
                    m.Coordinates = offsets[buffer[n]];

                } else {
                    Signal o = original.Clone();
                    o.Coordinates = offsets[buffer[n]];
                    o.Color = new Color(0);
                    InvokeExit(o);

                    if (m.Color.Lit) {
                        if (offsets.Count > 1) {
                            int old = buffer[n];
                            buffer[n] = RNG.Next(offsets.Count - 1);
                            if (buffer[n] >= old) buffer[n]++;
                        }
                        m.Coordinates = offsets[buffer[n]];
                    
                    } else buffer.Remove(n, out int _);
                }

                if (buffer.ContainsKey(n)) {
                    InvokeExit(m);
                    FireCourier((original, offsets), _time * _gate);
                } else {
                    timers[n].Dispose();
                    timers.Remove(n, out Courier _);
                }
            }
        }

        public override void MIDIProcess(Signal n) {
            if (n.Index == 100 || ((n.Coordinates.X >= 4 && n.Coordinates.X <= 5) && n.Coordinates.Y == -1)) {
                InvokeExit(n);
                return;
            }

            double startX = n.Coordinates.X;
            double startY = n.Coordinates.Y;

            List<DoubleTuple> validOffsets = new List<DoubleTuple>() {n.Coordinates};
            List<DoubleTuple> interpolatedOffsets = new List<DoubleTuple>() {n.Coordinates};

            for (int i = 0; i < Offsets.Count; i++) {
                Offsets[i].Apply(n.Coordinates, n.Source.PadLayout.bounds, Wrap, out DoubleTuple newCoords);
                validOffsets.Add(newCoords);

                if (CopyMode == CopyType.Interpolate) {
                    double angle = Angles[i] / 90.0 * Math.PI;
                    
                    double endX = newCoords.X;
                    double endY = newCoords.Y;
                    
                    int pointCount;
                    Func<int, DoubleTuple> pointGenerator;

                    DoubleTuple source = n.Coordinates;
                    DoubleTuple target = newCoords;

                    if (angle != 0) {
                        // https://www.desmos.com/calculator/hizsxmojxz

                        double diam = Math.Sqrt(Math.Pow(startX - endX, 2) + Math.Pow(startY - endY, 2));
                        double commonTan = Math.Atan((startX - endX) / (endY - startY));

                        double cord = diam / (2 * Math.Tan(Math.PI - angle / 2)) * (((endY - startY) >= 0)? 1 : -1);
                        
                        DoubleTuple center = new DoubleTuple(
                            (startX + endX) / 2 + Math.Cos(commonTan) * cord,
                            (startY + endY) / 2 + Math.Sin(commonTan) * cord
                        );
                        
                        double radius = diam / (2 * Math.Sin(Math.PI - angle / 2));
                        
                        double u = (Convert.ToInt32(angle < 0) * (Math.PI + angle) + Math.Atan2(startY - center.Y, startX - center.X)) % (2 * Math.PI);
                        double v = (Convert.ToInt32(angle < 0) * (Math.PI - angle) + Math.Atan2(endY - center.Y, endX - center.X)) % (2 * Math.PI);
                        v += (u <= v)? 0 : 2 * Math.PI;
                        
                        double startAngle = (angle < 0)? v : u;
                        double endAngle = (angle < 0)? u : v;

                        pointCount = (int)(Math.Abs(radius) * Math.Abs(endAngle - startAngle) * 5);
                        pointGenerator = t => CircularInterp(center, radius, startAngle, endAngle, t, pointCount);
                        
                    } else {
                        DoubleTuple p = new DoubleTuple(startX, startY);

                        DoubleTuple d = new DoubleTuple(
                            endX - startX,
                            endY - startY
                        );

                        DoubleTuple a = d.Apply(v => Math.Abs(v));
                        DoubleTuple b = d.Apply(v => (v < 0)? -1 : 1);

                        pointCount = (int)Math.Round(Math.Max(a.X, a.Y));
                        pointGenerator = t => LineGenerator(p, a, b, t);
                    }
                    
                    int lastPad = -1;
                    int currentPad;
                    for (int p = 1; p <= pointCount; p++) {
                        DoubleTuple doublepoint = pointGenerator.Invoke(p);
                        
                        if(Wrap){
                            doublepoint = Offset.Wrap(doublepoint, n.Source.PadLayout.bounds);
                        }
                        
                        if((currentPad = n.Source.PadLayout.PadAtCoords(doublepoint)) != -1 && currentPad != lastPad){
                            interpolatedOffsets.Add(doublepoint);
                            lastPad =  currentPad;
                        }
                    }
                }
                
                startX = newCoords.X;
                startY = newCoords.Y;
            }

            if (CopyMode == CopyType.Interpolate) validOffsets = interpolatedOffsets;

            if (CopyMode == CopyType.Static) {
                InvokeExit(n.Clone());

                for (int i = 1; i < validOffsets.Count; i++) {
                    Signal m = n.Clone();
                    m.Coordinates = validOffsets[i];

                    InvokeExit(m);
                }

            } else if (CopyMode == CopyType.Animate || CopyMode == CopyType.Interpolate) {
                if (!locker.ContainsKey(n)) locker[n] = new object();
                
                lock (locker[n]) {
                    if (Reverse) validOffsets.Reverse();

                    if (validOffsets[0] != null)
                        InvokeExit(n.With(validOffsets[0], n.Color));
                    
                    PolyInfo info = new PolyInfo(n, validOffsets);
                    poly.Add(info);

                    double total = _time * _gate * (validOffsets.Count - 1);

                    for (int i = 1; i < validOffsets.Count; i++)
                        if (!Infinite || i < validOffsets.Count - 1 || n.Color.Lit)
                            FireCourier(info, ApplyPinch(_time * _gate * i, total));
                }

            } else if (CopyMode == CopyType.RandomSingle) {
                Signal m = n.Clone();
                n.Color = new Color();

                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = RNG.Next(validOffsets.Count);
                    m.Coordinates = validOffsets[buffer[n]];

                } else {
                    m.Coordinates = validOffsets[buffer[n]];
                    if (!m.Color.Lit) buffer.Remove(n, out int _);
                }

                InvokeExit(m);

            } else if (CopyMode == CopyType.RandomLoop) HandleRandomLoop(n, validOffsets);
        }

        protected override void Stop() {
            foreach (Courier i in timers.Values) i.Dispose();
            timers.Clear();

            foreach (PolyInfo info in poly) {
                foreach (Courier i in info.timers) i.Dispose();
                info.timers.Clear();
            }
            poly.Clear();

            buffer.Clear();
            locker.Clear();
        }

        public override void Dispose() {
            if (Disposed) return;

            Stop();

            foreach (Offset offset in Offsets) offset.Dispose();
            Time.Dispose();
            base.Dispose();
        }
    
        DoubleTuple CircularInterp(DoubleTuple center, double radius, double startAngle, double endAngle, double t, double pointCount) {
            double angle = startAngle + (endAngle - startAngle) * ((double)t / pointCount);

            return new DoubleTuple(
                (double)(center.X + radius * Math.Cos(angle)),
                (double)(center.Y + radius * Math.Sin(angle))
            );
        }
        
        DoubleTuple LineGenerator(DoubleTuple p, DoubleTuple a, DoubleTuple b, int t) => (a.X > a.Y)
            ? new DoubleTuple(
                (p.X + t * b.X),
                (p.Y + (int)Math.Round((t / a.X * a.Y)) * b.Y)
            )
            : new DoubleTuple(
                (p.X + (int)Math.Round((t / a.Y * a.X)) * b.X),
                (p.Y + t * b.Y)
            );
    }
}


