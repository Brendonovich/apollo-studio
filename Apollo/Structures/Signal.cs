using System;
using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;

namespace Apollo.Structures {
    public class Signal {
        public object Origin;
        public Launchpad Source;
        int _index = 11;
        int[] _macros;
        public Color Color;
        public int Layer;
        public BlendingType BlendingMode;
        int _range = 200;
        public Stack<int> MultiTarget = new Stack<int>();
        public bool HashIndex = true;
        public DoubleTuple Coordinates;

        public int Index = -1;
        
        public int[] Macros {
            get => _macros;
            set => _macros = value?? Program.Project?.Macros?? new int[] {1, 1, 1, 1};
        }

        public int GetMacro(int target) => Macros[target - 1];
        
        public void SetMacro(int target, int value) => Macros[target] = value;

        public int BlendingRange {
            get => _range;
            set {
                if (1 <= value && value <= 200)
                    _range = value;
            }
        }

        public int? PeekMultiTarget => MultiTarget.TryPeek(out int target)? (int?)target : null;

        public Stack<int> CopyMultiTarget() => new Stack<int>(MultiTarget.ToArray());

        public Signal Clone() => new Signal(Origin, Source, Index, Color.Clone(), (int[])Macros?.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget(), Coordinates.Clone()) {
            HashIndex = HashIndex
        };

        public Signal With(byte index = 11, Color color = null) => new Signal(Origin, Source, index, color, (int[])Macros.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget());

        public Signal With(DoubleTuple coords = null, Color color = null) => new Signal(Origin, Source, 11, color, (int[])Macros.Clone(), Layer, BlendingMode, BlendingRange, CopyMultiTarget(), coords);

        public Signal(object origin, Launchpad source, int index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null, DoubleTuple coords = null) {
            Origin = origin;
            Source = source;
            Index = index;
            Color = color?? new Color();
            Macros = macros;
            Layer = layer;
            BlendingMode = blending;
            BlendingRange = blendingrange;
            MultiTarget = multiTarget?? new Stack<int>();
            Coordinates = coords?? new DoubleTuple(0, 0);
        }

        public Signal(InputType input, object origin, Launchpad source, byte index = 11, Color color = null, int[] macros = null, int layer = 0, BlendingType blending = BlendingType.Normal, int blendingrange = 200, Stack<int> multiTarget = null, DoubleTuple coords = null): this(
            origin,
            source,
            (input == InputType.DrumRack)? Converter.DRtoXY(index) : ((index == 99)? (byte)100 : index),
            color,
            macros,
            layer,
            blending,
            blendingrange,
            multiTarget,
            coords
        ) {}

        public override bool Equals(object obj) {
            if (!(obj is Signal)) return false;
            return this == (Signal)obj;
        }

        public static bool operator ==(Signal a, Signal b) => a.Source == b.Source && ((a.HashIndex && b.HashIndex)? a.Index == b.Index : true) && a.Color == b.Color && a.Macros.SequenceEqual(b.Macros) && a.Layer == b.Layer && a.BlendingMode == b.BlendingMode && a.PeekMultiTarget == b.PeekMultiTarget;
        public static bool operator !=(Signal a, Signal b) => !(a == b);
        
        public override int GetHashCode() => HashCode.Combine(Source, HashIndex? Index : 11, Color, HashCode.Combine(Macros[0], Macros[1], Macros[2], Macros[3]), Layer, BlendingMode, BlendingRange, HashCode.Combine(PeekMultiTarget, Coordinates));
        
        public override string ToString() => $"{((Source == null)? "null" : Source.Name)} -> {Index} @ {Layer} + {BlendingMode} & {MultiTarget} = {Color}";
    }

    public class StopSignal: Signal {
        public StopSignal(): base(null, null) {}
    }
}