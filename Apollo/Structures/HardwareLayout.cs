using System.Collections.Generic;
using System.Linq;
using Apollo;

namespace Apollo.Structures{
    public class HardwareLayout{
        
        List<Pad> _pads;
        public List<Pad> Pads{
            get => _pads;
            set{
                _pads = value;
                PadXMeta = new Dictionary<double, int>();
                foreach(Pad pad in _pads){
                    double x = pad.x + pad.width;
                    if(!PadXMeta.ContainsKey(x)) PadXMeta.Add(x, 0);
                    PadXMeta[x] += 1;
                }
            }
        }
        Dictionary<double, int> PadXMeta;
        
        public Bounds bounds;
        
        public static HardwareLayout GenerateMK2Layout(){
            HardwareLayout layout = new HardwareLayout();
            List<Pad> pads = new List<Pad>();
            
            for(int i = 0; i < 8; i++){
                for(int j = 0; j < 8; j++){
                    pads.Add(new Pad(-3.92 + j, -3.92 + i, 0.83, 0.83, false, (byte)(11 + j + (10 * i))));
                }
            }
            
            for(int j = 0; j < 8; j++){
                pads.Add(new Pad(4.2, -3.8 + j, 0.583, 0.583, true, (byte)(19 + (j * 10))));
            }
            
            for(int j = 0; j < 8; j++){
                pads.Add(new Pad(-3.8 + j, 4.2, 0.583, 0.583, true, (byte)(104 + j)));
            }
            
            pads = pads.OrderBy(pad => pad.x).ToList();
            
            layout.Pads = pads;
            layout.bounds = new Bounds(-4.5, -4.5, 10, 10);
            
            return layout;
        }
        
        public int PadAtCoords(DoubleTuple coords){
            int skip = 0;
            
            foreach(double key in PadXMeta.Keys){
                if(key < coords.X) skip += PadXMeta[key];
            }
            
            for(int i = skip; i < Pads.Count; i++){
                if (Pads[i].Check(coords)) return i;
            }
            return -1;
        }
    }  
    
    public struct Bounds{
        public double X;
        public double Y;
        public double Width;
        public double Height;
        
        public Bounds(double x = 0, double y = 0, double width = 0, double height = 0){
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
        
    
    public class Pad{
        public double x;
        public double y;
        
        public double width;
        public double height;
        
        public bool isCircle;
        public byte midiIndex;
        public Color color;
        
        public Pad(double _x = 0, double _y = 0, double _width = 0, double _height = 0, bool _isCircle = false, byte _midiIndex = 0){
            x = _x;
            y = _y;
            width = _width;
            height = _height;
            isCircle = _isCircle;
            midiIndex = _midiIndex;
        }
        
        public bool Check(DoubleTuple coords){
            if(!isCircle){
                if(coords.X >= x && coords.X <= x + width && coords.Y >= y && coords.Y <= y + height) return true;
            } else {
                DoubleTuple center = new DoubleTuple(x + width / 2, y + height / 2);
                if(coords.DistanceTo(center) <= width/2) return true;
            }
            
            return false;
        }
    }

    
}
