using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Copy: Device {
        public static readonly new string DeviceIdentifier = "copy";

        public enum CopyType {
            Static,
            Animate,
            Interpolate,
            RandomSingle,
            RandomLoop
        }
        
        private Random RNG = new Random();

        public List<Offset> Offsets;

        public void Insert(int index, Offset offset = null) {
            Offsets.Insert(index, offset?? new Offset());
            Offsets.Last().Changed += OffsetChanged;

            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Insert(index, Offsets[index]);
        }

        public void Remove(int index, bool dispose = true) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Offsets[index].Changed -= OffsetChanged;
            Offsets.RemoveAt(index);
        }

        private void OffsetChanged(Offset sender) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetOffset(Offsets.IndexOf(sender), sender.X, sender.Y);
        }

        private Time _time;
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

        private void FreeChanged(int value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateValue(value);
        }

        private void ModeChanged(bool value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetMode(value);
        }

        private void StepChanged(Length value) {
            if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetRateStep(value);
        }

        private decimal _gate;
        public decimal Gate {
            get => _gate;
            set {
                if (0.01M <= value && value <= 4) {
                    _gate = value;
                    
                    if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        private bool _wrap;
        public bool Wrap {
            get => _wrap;
            set {
                _wrap = value;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetWrap(Wrap);
            }
        }

        private class PolyInfo {
            public Signal n;
            public int index = 0;
            public object locker = new object();
            public List<int> offsets;

            public PolyInfo(Signal init_n, List<int> init_offsets) {
                n = init_n;
                offsets = init_offsets;
            }
        }

        public string CopyMode {
            get {
                if (_copymode == CopyType.Static) return "Static";
                else if (_copymode == CopyType.Animate) return "Animate";
                else if (_copymode == CopyType.Interpolate) return "Interpolate";
                else if (_copymode == CopyType.RandomSingle) return "Random Single";
                else if (_copymode == CopyType.RandomLoop) return "Random Loop";
                return null;
            }
            set {
                if (value == "Static") _copymode = CopyType.Static;
                else if (value == "Animate") _copymode = CopyType.Animate;
                else if (value == "Interpolate") _copymode = CopyType.Interpolate;
                else if (value == "Random Single") _copymode = CopyType.RandomSingle;
                else if (value == "Random Loop") _copymode = CopyType.RandomLoop;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetCopyMode(CopyMode);
            }
        }

        CopyType _copymode;

        public CopyType GetCopyMode() => _copymode;
        
        public enum GridType {
            Full,
            Square
        }

        public string GridMode {
            get {
                if (_gridmode == GridType.Full) return "10x10";
                else if (_gridmode == GridType.Square) return "8x8";
                return null;
            }
            set {
                if (value == "10x10") _gridmode = GridType.Full;
                else if (value == "8x8") _gridmode = GridType.Square;

                if (Viewer?.SpecificViewer != null) ((CopyViewer)Viewer.SpecificViewer).SetGridMode(GridMode);
            }
        }

        GridType _gridmode;
        
        public GridType GetGridMode() => _gridmode;

        private ConcurrentDictionary<Signal, int> buffer = new ConcurrentDictionary<Signal, int>();
        private ConcurrentDictionary<Signal, object> locker = new ConcurrentDictionary<Signal, object>();
        private ConcurrentDictionary<Signal, Courier> timers = new ConcurrentDictionary<Signal, Courier>();
        private HashSet<PolyInfo> poly = new HashSet<PolyInfo>();

        public override Device Clone() => new Copy(_time.Clone(), _gate, _copymode, _gridmode, Wrap, (from i in Offsets select i.Clone()).ToList()) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Copy(Time time = null, decimal gate = 1, CopyType copymode = CopyType.Static, GridType gridmode = GridType.Full, bool wrap = false, List<Offset> offsets = null): base(DeviceIdentifier) {
            Time = time?? new Time(true, null, 500);
            Gate = gate;
            _copymode = copymode;
            _gridmode = gridmode;
            Wrap = wrap;
            Offsets = offsets?? new List<Offset>();
        }
        
        private int ApplyWrap(int coord) => (_gridmode == GridType.Square)? ((coord + 7) % 8 + 1) : (coord + 10) % 10;

        private bool ApplyOffset(int index, Offset offset, out int x, out int y, out int result) {
            x = index % 10;
            y = index / 10;

            if (_gridmode == GridType.Square && (x == 0 || x == 9 || y == 0 || y == 9)) {
                result = 0;
                return false;
            }

            x += offset.X;
            y += offset.Y;

            return Validate(x, y, out result);
        }

        private bool Validate(int x, int y, out int result) {
            if (Wrap) {
                x = ApplyWrap(x);
                y = ApplyWrap(y);
            }

            result = y * 10 + x;

            if (_gridmode == GridType.Full) {
                if (0 <= x && x <= 9 && 0 <= y && y <= 9 && 1 <= result && result <= 98 && result != 9 && result != 90)
                    return true;
                
                if (y == -1 && 4 <= x && x <= 5) {
                    result = 99;
                    return true;
                }

            } else if (_gridmode == GridType.Square)
                if (1 <= x && x <= 8 && 1 <= y && y <= 8)
                    return true;
             
            return false;
        }

        private void FireCourier(Signal n, decimal time) {
            Courier courier = new Courier() {
                Info = n,
                AutoReset = false,
                Interval = (double)time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireCourier(PolyInfo info, decimal time) {
            Courier courier = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = (double)time,
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void FireCourier((Signal n, List<int>) info, decimal time) {
            Courier courier = timers[info.n] = new Courier() {
                Info = info,
                AutoReset = false,
                Interval = (double)time
            };
            courier.Elapsed += Tick;
            courier.Start();
        }

        private void Tick(object sender, EventArgs e) {
            if (Disposed) return;

            Courier courier = (Courier)sender;
            courier.Elapsed -= Tick;

            Type infoType = courier.Info.GetType();
            
            if ((_copymode == CopyType.Animate || _copymode == CopyType.Interpolate) && infoType == typeof(PolyInfo)) {
                PolyInfo info = (PolyInfo)courier.Info;

                lock (info.locker) {
                    if (++info.index < info.offsets.Count && info.offsets[info.index] != -1) {
                        Signal m = info.n.Clone();
                        m.Index = (byte)info.offsets[info.index];
                        MIDIExit?.Invoke(m);
                        
                    } else poly.Remove(info);
                }

            } else if (_copymode == CopyType.RandomLoop && infoType == typeof((Signal, List<int>))) {
                (Signal n, List<int> offsets) = ((Signal, List<int>))courier.Info;
                HandleRandomLoop(n, offsets);
            }
        }

        private void HandleRandomLoop(Signal original, List<int> offsets) {
            Signal n = original.Clone();
            Signal m = original.Clone();
            n.Color = new Color();

            if (!locker.ContainsKey(n)) locker[n] = new object();

            lock (locker[n]) {
                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = RNG.Next(offsets.Count);
                    m.Index = (byte)offsets[buffer[n]];

                } else {
                    Signal o = original.Clone();
                    o.Index = (byte)offsets[buffer[n]];
                    o.Color = new Color(0);
                    MIDIExit?.Invoke(o);

                    if (m.Color.Lit) {
                        if (offsets.Count > 1) {
                            int old = buffer[n];
                            buffer[n] = RNG.Next(offsets.Count - 1);
                            if (buffer[n] >= old) buffer[n]++;
                        }
                        m.Index = (byte)offsets[buffer[n]];
                    
                    } else buffer.Remove(n, out int _);
                }

                if (buffer.ContainsKey(n)) {
                    MIDIExit?.Invoke(m);
                    FireCourier((original, offsets), _time * _gate);
                } else {
                    timers[n].Dispose();
                    timers.Remove(n, out Courier _);
                }
            }
        }

        public override void MIDIProcess(Signal n) {
            int px = n.Index % 10;
            int py = n.Index / 10;

            List<int> validOffsets = new List<int>() {n.Index};
            List<int> interpolatedOffsets = new List<int>() {n.Index};

            for (int i = 0; i < Offsets.Count; i++) {
                if (ApplyOffset(n.Index, Offsets[i], out int x, out int y, out int result))
                    validOffsets.Add(result);

                if (_copymode == CopyType.Interpolate) {
                    List<(int X, int Y)> points = new List<(int, int)>();

                    int dx = x - px;
                    int dy = y - py;

                    int ax = Math.Abs(dx);
                    int ay = Math.Abs(dy);

                    int bx = (dx < 0)? -1 : 1;
                    int by = (dy < 0)? -1 : 1;

                    if (ax > ay) for (int j = 1; j <= ax; j++)
                        points.Add((px + j * bx, py + (int)Math.Round((double)j / ax * ay) * by));

                    else for (int j = 1; j <= ay; j++)
                        points.Add((px + (int)Math.Round((double)j / ay * ax) * bx, py + j * by));
                    
                    foreach ((int ix, int iy) in points)
                        interpolatedOffsets.Add(Validate(ix, iy, out int iresult)? iresult : -1);
                }

                px = x;
                py = y;
            }

            if (_copymode == CopyType.Interpolate) validOffsets = interpolatedOffsets;

            if (_copymode == CopyType.Static) {
                MIDIExit?.Invoke(n);

                for (int i = 1; i < validOffsets.Count; i++) {
                    Signal m = n.Clone();
                    m.Index = (byte)validOffsets[i];

                    MIDIExit?.Invoke(m);
                }

            } else if (_copymode == CopyType.Animate || _copymode == CopyType.Interpolate) {
                if (!locker.ContainsKey(n)) locker[n] = new object();
                
                lock (locker[n]) {
                    MIDIExit?.Invoke(n.Clone());
                    
                    PolyInfo info = new PolyInfo(n, validOffsets);
                    poly.Add(info);

                    for (int i = 1; i < validOffsets.Count; i++)
                        FireCourier(info, _time * _gate * i);
                }

            } else if (_copymode == CopyType.RandomSingle) {
                Signal m = n.Clone();
                n.Color = new Color();

                if (!buffer.ContainsKey(n)) {
                    if (!m.Color.Lit) return;
                    buffer[n] = m.Index = (byte)validOffsets[RNG.Next(validOffsets.Count)];

                } else {
                    m.Index = (byte)buffer[n];
                    if (!m.Color.Lit) buffer.Remove(n, out int _);
                }

                MIDIExit?.Invoke(m);

            } else if (_copymode == CopyType.RandomLoop) HandleRandomLoop(n, validOffsets);
        }

        public override void Dispose() {
            foreach (Offset offset in Offsets) offset.Dispose();
            Time.Dispose();
            base.Dispose();
        }
    }
}