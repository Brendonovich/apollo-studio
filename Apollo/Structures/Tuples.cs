using System;

namespace Apollo.Structures{
    public class DoubleTuple {
        public double X;
        public double Y;

        public DoubleTuple(double x = 0, double y = 0) {
            X = x;
            Y = y;
        }

        public DoubleTuple Round() => new DoubleTuple((int)Math.Round(X), (int)Math.Round(Y));
        public DoubleTuple Clone() => new DoubleTuple(X, Y);

        public static DoubleTuple operator *(DoubleTuple t, double f) => new DoubleTuple((double)(t.X * f), (double)(t.Y * f));
        public static DoubleTuple operator +(DoubleTuple a, DoubleTuple b) => new DoubleTuple((double)(a.X + b.X), (double)(a.Y + b.Y));
        
        public DoubleTuple Apply(Func<double, double> action) => new DoubleTuple(
            action.Invoke(X),
            action.Invoke(Y)
        );
    };

    public class IntTuple {
        public int X;
        public int Y;

        public IntTuple(int x, int y) {
            X = x;
            Y = y;
        }

        public IntTuple Apply(Func<int, int> action) => new IntTuple(
            action.Invoke(X),
            action.Invoke(Y)
        );
    }
}