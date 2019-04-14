﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class PreviewViewer: UserControl {
        public static readonly string DeviceIdentifier = "preview";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Preview _preview;
        LaunchpadGrid Grid;

        private SolidColorBrush GetColor(bool value) => (SolidColorBrush)Application.Current.Styles.FindResource(value? "ThemeAccentBrush" : "ThemeForegroundLowBrush");

        public PreviewViewer(Preview preview) {
            InitializeComponent();

            _preview = preview;
            _preview.SignalExited += HandleRender;

            Grid = this.Get<LaunchpadGrid>("Grid");

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), new SolidColorBrush(new AvaloniaColor(255, 0, 0, 0)));
        }

        private void PadChanged(int index, bool state) => _preview.MIDIEnter(new Signal(Track.Get(_preview).Launchpad, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 63 : 0))));
        private void PadPressed(int index) => PadChanged(index, true);
        private void PadReleased(int index) => PadChanged(index, false);

        private void SignalRender(Signal n) => Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), (SolidColorBrush)n.Color.ToBrush());

        private void HandleRender(Signal n) => Dispatcher.UIThread.InvokeAsync(() => { SignalRender(n); });
    }
}