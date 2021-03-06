﻿using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Interfaces;
using Apollo.Structures;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.Elements {
    public class Track: ISelect, IChainParent {
        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => Program.Project;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }

        public TrackInfo Info;
        public TrackWindow Window;
        
        public bool IsDisposing { get; private set; } = false;

        public delegate void ParentIndexChangedEventHandler(int index);
        public event ParentIndexChangedEventHandler ParentIndexChanged;
        
        public delegate void NameChangedEventHandler(string name);
        public event NameChangedEventHandler NameChanged;

        public delegate void DisposingEventHandler();
        public event DisposingEventHandler Disposing;

        int? _ParentIndex;
        public int? ParentIndex {
            get => _ParentIndex;
            set {
                if (_ParentIndex != value) {
                    _ParentIndex = value;
                    ParentIndexChanged?.Invoke(_ParentIndex.Value);
                }
            }
        }

        public static Track Get(Device device) => (device.Parent?.Parent != null)
            ? ((device.Parent?.Parent is Track track)
                ? track
                : Get((Device)device.Parent?.Parent)
            ) : null;

        public static Track Get(Chain chain) => (chain.Parent != null)
            ? ((chain.Parent is Track track)
                ? track
                : Get((Device)chain.Parent)
            ) : null;

        public static bool PathContains(ISelect child, List<ISelect> search) {
            ISelect last = child;

            while (true) {
                if (last is Chain chain && chain.IRoot)
                    last = (ISelect)chain.Parent;

                if (search.Contains(last)) return true;

                if (last is Track) return false;
                
                last = (last is Chain _chain && _chain.Parent is Choke)  // Choke isn't an ISelectParent so IParent won't work!
                    ? (ISelect)((Chain)last).Parent
                    : (ISelect)last.IParent;
            }
        }

        public static List<int> GetPath(ISelect child) {
            List<int> path = new List<int>();
            ISelect last = child;

            while (true) {
                if (last is Chain chain && (chain.Parent is Choke || chain.IRoot))
                    last = (ISelect)chain.Parent;

                path.Add(last.IParentIndex?? -1);

                if (last is Track) break;

                last = (ISelect)last.IParent;
            }

            return path;
        }

        public static T TraversePath<T>(List<int> path) where T : ISelect {
            ISelectParent ret = Program.Project[path.Last()].Chain;

            if (path.Count == 1) return (T)ret;

            for (int i = path.Count - 2; i > 0; i--)
                if (path[i] == -1) ret = ((Multi)ret).Preprocess;
                else if (ret.IChildren[path[i]] is Choke choke) ret = choke.Chain;
                else ret = (ISelectParent)ret.IChildren[path[i]];

            if (path[0] == -1) return (T)(ISelect)((Multi)ret).Preprocess;
            else if (ret.IChildren[path[0]] is Choke choke && typeof(T) != typeof(Choke)) return (T)(ISelect)choke.Chain;
            else return (T)ret.IChildren[path[0]];
        }

        public Chain Chain;
        Launchpad _launchpad;

        public Launchpad Launchpad {
            get => _launchpad;
            set {
                if (_launchpad != null) _launchpad.Receive -= MIDIEnter;

                if (value == null) value = MIDI.NoOutput;
                _launchpad = value;

                if (_launchpad != null) _launchpad.Receive += MIDIEnter;

                Info?.UpdatePorts();
            }
        }
        
        string _name;
        public string Name {
            get => _name;
            set {
                _name = value;
                NameChanged?.Invoke(ProcessedName);
                Info?.SetName(_name);
            }
        }

        public string ProcessedName {
            get {
                string ret = "";
                for (int i = 0; i < _name.Length; i++)
                    ret += (_name[i] == '#' && (i == 0 || _name[i - 1] == ' ') && (i == _name.Length - 1 || _name[i + 1] == ' '))? (ParentIndex + 1).ToString() : _name[i].ToString();

                return ret;
            }
        }

        bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;

                    Info?.SetEnabled();
                    Window?.SetEnabled();
                }
            }
        }

        public Track Clone() => new Track(Chain.Clone(), null, Name) {
            Enabled = Enabled
        };

        public Track(Chain init = null, Launchpad launchpad = null, string name = "Track #") {
            Chain = init?? new Chain();
            Chain.Parent = this;
            Chain.MIDIExit = ChainExit;

            Launchpad = launchpad;
            Name = name;
        }

        void ChainExit(Signal n) => n.Source?.Render(n);

        void MIDIEnter(Signal n) {
            if (Enabled) Chain?.MIDIEnter(n);
        }

        public void Dispose() {
            IsDisposing = true;

            Disposing?.Invoke();
            Disposing = null;
            NameChanged = null;
            ParentIndexChanged = null;

            Window?.Close();
            Window = null;
            Info = null;
            _ParentIndex = null;
            
            Chain?.Dispose();
            Chain = null;

            if (Launchpad != null) Launchpad.Receive -= MIDIEnter;
        }

        public static bool Move(List<Track> source, Project target, int position, bool copy = false) => (position == -1)
            ? Move(source, target, copy)
            : Move(source, target[position], copy);

        public static bool Move(List<Track> source, Track target, bool copy = false) {
            if (!copy && (source.Contains(target) || source[0].ParentIndex == target.ParentIndex + 1))
                return false;
            
            List<Track> moved = new List<Track>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) Program.Project.Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                Program.Project.Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            Program.Project.Window.Selection.Select(moved.First());
            Program.Project.Window.Selection.Select(moved.Last(), true);
            
            return true;
        }

        public static bool Move(List<Track> source, Project target, bool copy = false) {
            if (!copy && target.Count > 0 && source[0] == target[0])
                return false;
            
            List<Track> moved = new List<Track>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) Program.Project.Remove(source[i].ParentIndex.Value, false);

                moved.Add(copy? source[i].Clone() : source[i]);

                Program.Project.Insert(i, moved.Last());
            }

            Program.Project.Window.Selection.Select(moved.First());
            Program.Project.Window.Selection.Select(moved.Last(), true);
            
            return true;
        }
    }
}