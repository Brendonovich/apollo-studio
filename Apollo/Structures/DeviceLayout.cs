using System.Collections.Generic;

using Apollo;

namespace Apollo.Structures{
    public class DeviceLayout{
        
        List<PadDef> _pads;
        public List<PadDef> Pads{
            get => _pads;
            set{
                _pads = value;
                PadYMeta = new Dictionary<double, int>();
                foreach(PadDef pad in _pads){
                    if(!PadYMeta.ContainsKey(pad.y + pad.height)) PadYMeta.Add(pad.y + pad.height, 0);
                    PadYMeta[pad.y + pad.height] += 1;
                }
            }
        }
        Dictionary<double, int> PadYMeta;
        
        public static List<PadDef> GenerateMK2Def(){
            List<PadDef> pads = new List<PadDef>();
            for(int i = 0; i < 9; i++){
                for(int j = 0; j < 9; j++){
                    if(i == 8 && j == 8) return pads;
                    
                    pads.Add(new PadDef(-3.875 + j, -3.875 + i, 0.75, 0.75, (i == 8 || j == 8), (byte)((i != 8)? (11 + j + (10 * i)) : (24 + j + (10 * i)))));
                }
            }
            return pads;
        }
        
        public byte PadAtCoords(DoubleTuple coords){
            int skip = 0;
            
            foreach(double key in PadYMeta.Keys){
                if(key < coords.Y) skip += PadYMeta[key];
            }
            
            for(int i = skip; i < Pads.Count; i++){
                if (Pads[i].Check(coords)) return Pads[i].midiIndex;
            }
            return 255;
        }
        
        public struct PadDef{
            public double x;
            public double y;
            public double width;
            public double height;
            public bool isCircle;
            public byte midiIndex;
            
            public PadDef(double _x = 0, double _y = 0, double _width = 0, double _height = 0, bool _isCircle = false, byte _midiIndex = 0){
                x = _x;
                y = _y;
                width = _width;
                height = _height;
                isCircle = _isCircle;
                midiIndex = _midiIndex;
            }
            
            public bool Check(DoubleTuple coords){
                if(coords.X >= x && coords.X <= x + width && coords.Y >= y && coords.Y <= y + height) return true;
                else return false;
            }
        }
    }  
}
