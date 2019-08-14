using System;
using System.Collections.Generic;

using RtMidi.Core;
using RtMidi.Core.Devices.Infos;

using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Core {
    public static class MIDI {
        public delegate void DevicesUpdatedEventHandler();
        public static event DevicesUpdatedEventHandler DevicesUpdated;

        public static void DoneIdentifying() => DevicesUpdated?.Invoke();

        static List<Launchpad> _devices = new List<Launchpad>();
        public static List<Launchpad> Devices {
            get => _devices;
            set {
                if (_devices != value) {
                    _devices = value;
                    
                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static readonly Launchpad NoOutput = new VirtualLaunchpad("No Output");

        public static void ClearState(bool multi = true) {
            foreach (Track track in Program.Project?.Tracks)
                track.Chain.MIDIEnter(new StopSignal());
            
            if (multi) Multi.InvokeReset();
            Preview.InvokeClear();

            foreach (Launchpad lp in MIDI.Devices)
                lp.Clear(true);
        }
        
        static Courier courier;
        static bool started = false;

        public static void Start() {
            if (started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            courier = new Courier() { Interval = 100 };
            courier.Elapsed += Rescan;
            courier.Start();
            started = true;
        }

        public static void Stop() {
            if (!started) return;

            if (!NoOutput.Available)
                NoOutput.Connect(null, null);

            courier.Dispose();
            started = false;
        }

        static object locker = new object();
        static bool updated = false;

        public static void Update() {
            lock (locker) {
                if (updated) {
                    updated = false;
                    DevicesUpdated?.Invoke();
                }
            }
        }

        public static VirtualLaunchpad ConnectVirtual() {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = 1; true; i++) {
                    string name = $"Virtual Launchpad {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is VirtualLaunchpad vlp && !vlp.Available) {
                            vlp.Connect(null, null);
                            updated = true;
                            return vlp;
                        }

                    } else {
                        Devices.Add(ret = new VirtualLaunchpad(name));
                        ret.Connect(null, null);
                        updated = true;
                        return (VirtualLaunchpad)ret;
                    }
                }
            }
        }

        public static AbletonLaunchpad ConnectAbleton(int version) {
            lock (locker) {
                Launchpad ret = null;
            
                for (int i = 1; true; i++) {
                    string name = $"Ableton Connector {i}";

                    ret = Devices.Find((lp) => lp.Name == name);
                    if (ret != null) {
                        if (ret is AbletonLaunchpad alp && !alp.Available) {
                            alp.Version = version;
                            alp.Connect(null, null);
                            updated = true;
                            return alp;
                        }

                    } else {
                        Devices.Add(ret = new AbletonLaunchpad(name) { Version = version });
                        ret.Connect(null, null);
                        updated = true;
                        return (AbletonLaunchpad)ret;
                    }
                }
            }
        }

        public static Launchpad Connect(string name, IMidiInputDeviceInfo input = null, IMidiOutputDeviceInfo output = null) {
            lock (locker) {
                Launchpad ret = null;

                foreach (Launchpad device in Devices) {
                    if (device.Name == name) {
                        ret = device;
                        updated |= !device.Available;

                        if (!device.Available)
                            device.Connect(input, output);
                        
                        return ret;
                    }
                }

                Devices.Add(ret = new Launchpad(name, input, output));
                updated = true;
                return ret;
            }
        }

        public static void Disconnect(Launchpad lp) {
            lock (locker) {
                if (lp.GetType() != typeof(VirtualLaunchpad))
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices)
                        if (lp.Name.Replace("MIDIIN", "") == $"{output.Name} ({output.Port})".Replace("MIDIOUT", "")) return;

                lp.Disconnect();
                updated = true;
            }
        }

        public static void Rescan(object sender, EventArgs e) {
            lock (locker) {
                foreach (IMidiInputDeviceInfo input in MidiDeviceManager.Default.InputDevices)
                    foreach (IMidiOutputDeviceInfo output in MidiDeviceManager.Default.OutputDevices) {
                        string in_name = $"{input.Name} ({input.Port})";
                        string out_name = $"{output.Name} ({output.Port})";
                        if (in_name.Replace("MIDIIN", "") == out_name.Replace("MIDIOUT", ""))
                            Connect(in_name, input, output);
                    }

                foreach (Launchpad device in Devices)
                    if (device.GetType() == typeof(Launchpad) && device.Available)
                        Disconnect(device);

                Program.Log($"Rescan");

                if (updated) Update();
            }
        }
    }
}
