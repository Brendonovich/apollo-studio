﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class RefreshViewer: UserControl {
        public static readonly string DeviceIdentifier = "refresh";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            Macros = ((StackPanel)Content).Children.OfType<CheckBox>().ToArray();
        }
        
        Refresh _refresh;
        CheckBox[] Macros = new CheckBox[4];

        public RefreshViewer() => new InvalidOperationException();

        public RefreshViewer(Refresh refresh) {
            InitializeComponent();

            _refresh = refresh;
            
            for (int i = 0; i < 4; i++)
                Macros[i].IsChecked = _refresh.GetMacro(i);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _refresh = null;

        void Macro_Changed(object sender, RoutedEventArgs e) {
            CheckBox source = (CheckBox)sender;
            int index = Array.IndexOf(Macros, source);
            bool value = source.IsChecked.Value;

            if (_refresh.GetMacro(index) != value) {
                bool u = _refresh.GetMacro(index);
                bool r = value;
                List<int> path = Track.GetPath(_refresh);

                Program.Project.Undo.Add($"Refresh Macro {index + 1} changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Refresh>(path).SetMacro(index, u);
                }, () => {
                    Track.TraversePath<Refresh>(path).SetMacro(index, r);
                });

                _refresh.SetMacro(index, value);
            }
        }

        public void SetMacro(int index, bool value) => Macros[index].IsChecked = value;
    }
}
