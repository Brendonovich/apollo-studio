using System;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Rotate: Device {
        double _angle;
        public double Angle {
            get => _angle;
            set {
                _angle = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetAngle(Angle);
            }
        }

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((RotateViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Rotate(Angle, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Rotate(double angle = 0, bool bypass = false): base("rotate") {
            Angle = angle;
            Bypass = bypass;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) InvokeExit(n.Clone());
            
            double x = n.Coordinates.X;
            double y = n.Coordinates.Y;
            
            double cos = Math.Cos(Angle);
            double sin = Math.Sin(Angle);
            
            Signal m = n.With(new DoubleTuple(x * cos + sin * y, y * cos - sin * x), n.Color);

            InvokeExit(m);
        }
    }
}