using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;

namespace Apollo.DeviceViewers {
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Angle = this.Get<Dial>("Angle");
            Bypass = this.Get<CheckBox>("Bypass");
        }
        
        Rotate _rotate;
        Dial Angle;
        CheckBox Bypass;

        public RotateViewer() => new InvalidOperationException();

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            Angle.RawValue = (int)(_rotate.Angle / Math.PI * 180);
            Bypass.IsChecked = _rotate.Bypass;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _rotate = null;
        
        public void Angle_Changed(Dial sender, double angle, double? old){
            if(old != null && old.Value != angle){
                List<int> path = Track.GetPath(_rotate);
                
                double u = old.Value / 180 * Math.PI;
                double r = angle / 180 * Math.PI;

                Program.Project.Undo.Add($"Rotate Angle Changed to {angle}{Angle.Unit}", () => {
                    Rotate rotate = Track.TraversePath<Rotate>(path);
                    rotate.Angle = u;

                }, () => {
                    Rotate rotate = Track.TraversePath<Rotate>(path);
                    rotate.Angle = r;
                });
            }
            
            _rotate.Angle = angle / 180 * Math.PI;
        }

        public void SetAngle(double angle) => Angle.RawValue = (int)(angle / Math.PI * 180);

        void Bypass_Changed(object sender, RoutedEventArgs e) {
            bool value = Bypass.IsChecked.Value;

            if (_rotate.Bypass != value) {
                bool u = _rotate.Bypass;
                bool r = value;
                List<int> path = Track.GetPath(_rotate);

                Program.Project.Undo.Add($"Rotate Bypass Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Rotate>(path).Bypass = u;
                }, () => {
                    Track.TraversePath<Rotate>(path).Bypass = r;
                });

                _rotate.Bypass = value;
            }
        }

        public void SetBypass(bool value) => Bypass.IsChecked = value;
    }
}
