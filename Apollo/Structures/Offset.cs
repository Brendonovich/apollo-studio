using Apollo.Enums;

namespace Apollo.Structures {
    public class Offset {
        public delegate void ChangedEventHandler(Offset sender);
        public event ChangedEventHandler Changed;

        double _x = 0;
        public double X {
            get => _x;
            set {
                if (_x != value) {
                    _x = value;
                    Changed?.Invoke(this);
                }
            }
        }

        double _y = 0;
        public double Y {
            get => _y;
            set {
                if (_y != value) {
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

        double _ax = 0.5;
        public double AbsoluteX {
            get => _ax;
            set {
                _ax = value;
                Changed?.Invoke(this);
            }
        }

        double _ay = 0.5;
        public double AbsoluteY {
            get => _ay;
            set {
                _ay = value;
                Changed?.Invoke(this);
            }
        }
        
        public Offset Clone() => new Offset(X, Y, IsAbsolute, AbsoluteX, AbsoluteY);

        public Offset(double x = 0, double y = 0, bool absolute = false, double ax = 0.5, double ay = 0.5) {
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
            newCoords = coords.Clone();
            
            if (IsAbsolute) {
                newCoords.X = AbsoluteX;
                newCoords.Y = AbsoluteY;
                return;
            } else{
                newCoords.X += X;
                newCoords.Y += Y;
            }
        }

        public void Dispose() => Changed = null;
    }
}