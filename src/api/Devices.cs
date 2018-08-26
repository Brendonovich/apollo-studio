using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using api;

namespace api.Devices {
    public abstract class Device {
        public abstract void MIDIEnter(Signal n);
        public abstract Device Clone();
        public Action<Signal> MIDIExit;
    }

    public class Group: Device {
        private List<Chain> _chains;

        private void ChainExit(Signal n) {
            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }

        public Chain this[int index] {
            get {
                return _chains[index];
            }
            set {
                _chains[index] = value;
                _chains[index].MIDIExit = ChainExit;
            }
        }

        public int Count {
            get {
                return _chains.Count;
            }
        }

        public override Device Clone() {
            Group ret = new Group();
            foreach (Chain chain in _chains)
                ret.Add(chain.Clone());
            return ret;
        }

        public void Insert(int index) {
            _chains.Insert(index, new Chain(ChainExit));
        }

        public void Insert(int index, Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Insert(index, chain);
        }

        public void Add() {
            _chains.Add(new Chain(ChainExit));
        }

        public void Add(Chain chain) {
            chain.MIDIExit = ChainExit;
            _chains.Add(chain);  
        }

        public void Add(Chain[] chains) {
            foreach (Chain chain in chains) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }     
        }

        public void Remove(int index) {
            _chains.RemoveAt(index);
        }

        public Group() {
            _chains = new List<Chain>();
            this.MIDIExit = null;
        }

        public Group(Chain[] init) {
            _chains = new List<Chain>();
            foreach (Chain chain in init) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }
            this.MIDIExit = null;
        }

        public Group(List<Chain> init) {
            _chains = init;
            foreach (Chain chain in _chains)
                chain.MIDIExit = ChainExit;
            this.MIDIExit = null;
        }

        public Group(Action<Signal> exit) {
            _chains = new List<Chain>();
            this.MIDIExit = exit;
        }

        public Group(Chain[] init, Action<Signal> exit) {
            _chains = new List<Chain>();
            foreach (Chain chain in init) {
                chain.MIDIExit = ChainExit;
                _chains.Add(chain);
            }
            this.MIDIExit = exit;
        }

        public Group(List<Chain> init, Action<Signal> exit) {
            _chains = init;
            foreach (Chain chain in _chains)
                chain.MIDIExit = ChainExit;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            foreach (Chain chain in _chains)
                chain.MIDIEnter(n);
        }
    }

    public class Pitch: Device {
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= value && value <= 128)
                    _offset = value;
            }
        }

        public override Device Clone() {
            return new Pitch(_offset);
        }

        public Pitch() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Pitch(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Pitch(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Pitch(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            int result = (int)(n.p) + _offset;

            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }

    public class Chord: Device {
        private int _offset;

        public int Offset {
            get {
                return _offset;
            }
            set {
                if (-128 <= value && value <= 128)
                    _offset = value;
            }
        }

        public override Device Clone() {
            return new Chord(_offset);
        }

        public Chord() {
            this._offset = 0;
            this.MIDIExit = null;
        }

        public Chord(int offset) {
            this.Offset = offset;
            this.MIDIExit = null;
        }

        public Chord(Action<Signal> exit) {
            this._offset = 0;
            this.MIDIExit = exit;
        }

        public Chord(int offset, Action<Signal> exit) {
            this.Offset = offset;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (this.MIDIExit != null)
                this.MIDIExit(n);
            
            int result = (int)(n.p) + _offset;
            
            if (result < 0) result = 0;
            if (result > 127) result = 127;

            n.p = (byte)(result);

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }

    public class Velocity: Device {
        private int _r, _g, _b;

        public int Red {
            get {
                return _r;
            }
            set {
                if (0 <= value && value <= 63) _r = value;
            }
        }

        public int Green {
            get {
                return _g;
            }
            set {
                if (0 <= value && value <= 63) _g = value;
            }
        }

        public int Blue {
            get {
                return _b;
            }
            set {
                if (0 <= value && value <= 63) _b = value;
            }
        }

        public override Device Clone() {
            return new Velocity(_r, _g, _b);
        }

        public Velocity() {
            this._r = 63;
            this._g = 63;
            this._b = 63;
            this.MIDIExit = null;
        }

        public Velocity(int red, int green, int blue) {
            this._r = red;
            this._g = green;
            this._b = blue;
            this.MIDIExit = null;
        }
        
        public Velocity(Action<Signal> exit) {
            this._r = 63;
            this._g = 63;
            this._b = 63;
            this.MIDIExit = exit;
        }
        
        public Velocity(int red, int green, int blue, Action<Signal> exit) {
            this._r = red;
            this._g = green;
            this._b = blue;
            this.MIDIExit = exit;
        }

        public override void MIDIEnter(Signal n) {
            if (n.r != 0 || n.g != 0 || n.b != 0) {
                n.r = (byte)this._r;
                n.g = (byte)this._g;
                n.b = (byte)this._b;
            }

            if (this.MIDIExit != null)
                this.MIDIExit(n);
        }
    }

    public class Delay: Device {
        private int _length; // milliseconds
        private Queue<Timer> _timers;
        private TimerCallback _timerexit;

        public int Length {
            get {
                return _length;
            }
            set {
                if (0 <= value)
                    _length = value;
            }
        }

        public override Device Clone() {
            return new Delay(_length);
        }

        public Delay() {
            this._length = 200;
            this.MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length) {
            this.Length = length;
            this.MIDIExit = null;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(Action<Signal> exit) {
            this._length = 200;
            this.MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length, Action<Signal> exit) {
            this.Length = length;
            this.MIDIExit = exit;
            _timers = new Queue<Timer>();
            _timerexit = new TimerCallback(Tick);
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(Signal)) {
                Signal n = (Signal)info;
      
                if (this.MIDIExit != null)
                    this.MIDIExit(n);
                
                _timers.Dequeue();
            }
        }

        public override void MIDIEnter(Signal n) {
            _timers.Enqueue(new Timer(_timerexit, n, _length, System.Threading.Timeout.Infinite));
        }
    }
}