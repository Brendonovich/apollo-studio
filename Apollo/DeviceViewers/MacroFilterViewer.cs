﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using UniformGrid = Avalonia.Controls.Primitives.UniformGrid;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class MacroFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "macrofilter";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            MacrosGrid = this.Get<UniformGrid>("MacrosGrid");
            MacroDial = this.Get<Dial>("MacroDial");
        }
        
        MacroFilter _filter;
        UniformGrid MacrosGrid;
        Dial MacroDial;

        void SetColor(MacroRectangle rect, bool value) => rect.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeExtraBrush" : "ThemeForegroundLowBrush");

        public MacroFilterViewer() => new InvalidOperationException();

        public MacroFilterViewer(MacroFilter filter) {
            InitializeComponent();

            _filter = filter;
            
            MacroDial.RawValue = _filter.Macro;

            for (int i = 0; i < MacrosGrid.Children.Count; i++) {
                MacroRectangle Rect = (MacroRectangle)MacrosGrid.Children[i];
                SetColor(Rect, _filter[i]);
                Rect.Index = i + 1;
            }
        }
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _filter = null;

        void Target_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_filter);

                Program.Project.Undo.Add($"MacroFilter Target Changed to {r}", () => {
                    Track.TraversePath<MacroFilter>(path).Macro = u;
                }, () => {
                    Track.TraversePath<MacroFilter>(path).Macro = r;
                });
            }

            _filter.Macro = (int)value;
        }
        
        public void SetMacro(int macro) => MacroDial.RawValue = macro;

        bool drawingState;
        bool[] old;        
        bool mouseHeld = false;
        IControl mouseOver = null;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) {
                mouseHeld = true;

                e.Pointer.Capture(MacrosGrid);
                MacrosGrid.Cursor = new Cursor(StandardCursorType.Hand);

                int index = MacrosGrid.Children.IndexOf((IControl)sender);
                drawingState = !_filter[index];
                old = _filter.Filter.ToArray();

                MouseMove(sender, e);
            }
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                MouseMove(sender, e);

                if (old != null) {
                    bool[] u = old.ToArray();
                    bool[] r = _filter.Filter.ToArray();
                    List<int> path = Track.GetPath(_filter);

                    Program.Project.Undo.Add($"MacroFilter Changed", () => {
                        Track.TraversePath<MacroFilter>(path).Filter = u.ToArray();
                    }, () => {
                        Track.TraversePath<MacroFilter>(path).Filter = r.ToArray();
                    });

                    old = null;
                }

                mouseHeld = false;
                mouseOver = null;

                e.Pointer.Capture(null);
                MacrosGrid.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        void MouseEnter(MacroRectangle rect) => SetColor(rect, _filter[MacrosGrid.Children.IndexOf(rect)] = drawingState);

        void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld) {
                IInputElement over = MacrosGrid.InputHitTest(e.GetPosition(MacrosGrid));

                if (over is Grid grid) over = grid.Parent;

                if (over is MacroRectangle rect) {
                    if (mouseOver == null || mouseOver != rect)
                        MouseEnter(rect);

                    mouseOver = rect;

                } else mouseOver = null;
            }
        }

        public void Set(bool[] filter) {
            for (int i = 0; i < 100; i++)
                SetColor((MacroRectangle)MacrosGrid.Children[i], filter[i]);
        }
    }
}
