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
        
        static double Wrap(double coord, GridType gridMode) => (gridMode == GridType.Square)? ((coord + 7) % 8 + 1) : (coord + 10) % 10;

        public static bool Validate(DoubleTuple coords, GridType gridMode, bool wrap, out DoubleTuple newCoords) {
            if (wrap) {
                coords = coords.Apply(l => Wrap(l, gridMode));
            }
            
            newCoords = coords;

            if (gridMode == GridType.Full) {
                if (-5 <= coords.X && coords.X <= 5 && -5 <= coords.Y && coords.Y <= 5)
                    return true;
                
                if (coords.Y == -1 && -5 <= coords.X && coords.X <= 5) {
                    return true;
                }

            } else if (gridMode == GridType.Square)
                if (-4 <= coords.X && coords.X <= 4 && -4 <= coords.Y && coords.Y <= 4)
                    return true;
             
            return false;
        }

        public bool Apply(DoubleTuple coords, GridType gridMode, bool wrap, out DoubleTuple newCoords) {
            if (IsAbsolute) {
                coords.X = AbsoluteX;
                coords.Y = AbsoluteY;
                return Validate(coords, gridMode, wrap, out newCoords);
            }

            newCoords = coords.Clone();

            if (gridMode == GridType.Square && (newCoords.X == 0 || newCoords.X == 9 || newCoords.Y == 0 || newCoords.Y == 9)) {
                return false;
            }

            newCoords.X += X;
            newCoords.Y += Y;

            return Validate(newCoords, gridMode, wrap, out newCoords);
        }

        public void Dispose() => Changed = null;
    }
}