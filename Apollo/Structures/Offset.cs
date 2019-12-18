using Apollo.Enums;

namespace Apollo.Structures {
    public class Offset {
        public delegate void ChangedEventHandler(Offset sender);
        public event ChangedEventHandler Changed;

        int _x = 0;
        public int X {
            get => _x;
            set {
                if (-9 <= value && value <= 9 && _x != value) {
                    _x = value;
                    Changed?.Invoke(this);
                }
            }
        }

        int _y = 0;
        public int Y {
            get => _y;
            set {
                if (-9 <= value && value <= 9 && _y != value) {
                    _y = value;
                    Changed?.Invoke(this);
                }
            }
        }

        bool _absolute = false;
        public bool IsAbsolute {
            get => _absolute;
            set {
                if (_absolute != value) {
                    _absolute = value;
                    Changed?.Invoke(this);
                }
            }
        }

        int _ax = 5;
        public int AbsoluteX {
            get => _ax;
            set {
                if (0 <= value && value <= 9 && _ax != value) {
                    _ax = value;
                    Changed?.Invoke(this);
                }
            }
        }

        int _ay = 5;
        public int AbsoluteY {
            get => _ay;
            set {
                if (0 <= value && value <= 9 && _ay != value) {
                    _ay = value;
                    Changed?.Invoke(this);
                }
            }
        }
        
        public Offset Clone() => new Offset(X, Y, IsAbsolute, AbsoluteX, AbsoluteY);

        public Offset(int x = 0, int y = 0, bool absolute = false, int ax = 5, int ay = 5) {
            X = x;
            Y = y;
            IsAbsolute = absolute;
            AbsoluteX = ax;
            AbsoluteY = ay;
        }
        
        public static DoubleTuple Wrap(DoubleTuple coord, Bounds gridBounds){ 
            if(coord.X < gridBounds.X){
                while(coord.X < gridBounds.X)
                    coord.X += gridBounds.Width;
            } else if(coord.X > gridBounds.X + gridBounds.Width){
                while(coord.X > gridBounds.X + gridBounds.Width)
                    coord.X -= gridBounds.Width;
            }
            
            if(coord.Y < gridBounds.Y){
                while(coord.Y < gridBounds.Y)
                    coord.Y += gridBounds.Height;
            } else if(coord.Y > gridBounds.Y + gridBounds.Height){
                while(coord.Y > gridBounds.Y + gridBounds.Height)
                    coord.Y -= gridBounds.Height;
            }
            
            return coord;
        }

        public void Apply(DoubleTuple coords, Bounds gridMode, bool wrap, out DoubleTuple newCoords) {
            if (IsAbsolute) {
                coords.X = AbsoluteX;
                coords.Y = AbsoluteY;
            }

            newCoords = coords.Clone();

            newCoords.X += X;
            newCoords.Y += Y;
        }

        public void Dispose() => Changed = null;
    }
}