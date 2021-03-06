using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Flip: Device {
        FlipType _mode;
        public FlipType Mode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetMode(Mode);
            }
        }

        bool _bypass;
        public bool Bypass {
            get => _bypass;
            set {
                _bypass = value;
                
                if (Viewer?.SpecificViewer != null) ((FlipViewer)Viewer.SpecificViewer).SetBypass(Bypass);
            }
        }

        public override Device Clone() => new Flip(Mode, Bypass) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Flip(FlipType mode = FlipType.Horizontal, bool bypass = false): base("flip") {
            Mode = mode;
            Bypass = bypass;
        }

        public override void MIDIProcess(Signal n) {
            if (Bypass) InvokeExit(n.Clone());
            
            int x = n.Index % 10;
            int y = n.Index / 10;

            if (Mode == FlipType.Horizontal) x = 9 - x;
            else if (Mode == FlipType.Vertical) y = 9 - y;

            else if (Mode == FlipType.Diagonal1) {
                int temp = x;
                x = y;
                y = temp;
            
            } else if (Mode == FlipType.Diagonal2) {
                x = 9 - x;
                y = 9 - y;

                int temp = x;
                x = y;
                y = temp;
            }

            int result = y * 10 + x;
            
            n.Index = (byte)result;
            InvokeExit(n);
        }
    }
}