﻿using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class HorizontalDial: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DialValueChangedEventHandler(double NewValue);
        public event DialValueChangedEventHandler ValueChanged;

        Canvas ArcCanvas;
        Path ArcBase, Arc;
        TextBlock TitleText, Display;

        private const double width = 43, height = 39;
        private const double radius = 18, stroke = 7;
        private const double strokeHalf = stroke / 2;

        private const double angle_start = 4 * Math.PI / 3;
        private const double angle_end = -1 * Math.PI / 3;
        private const double angle_center = Math.PI / 2;

        private double ToValue(double rawValue) => Math.Pow((rawValue - _min) / (_max - _min), 1 / _exp);
        private double ToRawValue(double value) => _min + (_max - _min) * Math.Pow(value, _exp);

        private double _min = 0;
        public double Minimum {
            get => _min;
            set {
                if (_min != value) {
                    _min = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _max = 100;
        public double Maximum {
            get => _max;
            set {
                if (_max != value) {
                    _max = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private int _round = 0;
        public int Round {
            get => _round;
            set {
                if (_round != value) {
                    _round = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _exp = 100;
        public double Exponent {
            get => _exp;
            set {
                if (_exp != value) {
                    _exp = value;
                    RawValue = ToRawValue(_value);
                }
            }
        }

        private double _value = 0.5;
        public double Value {
            get => _value;
            set {
                value = Math.Max(0, Math.Min(1, value));
                if (value != _value) {
                    _value = value;
                    RawValue = ToRawValue(_value);
                    DrawArcValue();
                }
            }
        }

        private double _raw = 50;
        public double RawValue {
            get => _raw;
            set {
                value = Math.Round(Math.Max(_min, Math.Min(_max, value)) * Math.Pow(10, _round), 0) / Math.Pow(10, _round);
                if (_raw != value) {
                    _raw = value;
                    Value = ToValue(_raw);
                    Display.Text = ValueString;
                    ValueChanged?.Invoke(_raw);
                }
            }
        }

        private string _title = "Dial";
        public string Title {
            get => _title;
            set {
                TitleText.Text = _title = value;
            }
        }

        private string _unit = "%";
        public string Unit {
            get => _unit;
            set {
                _unit = value;
                DrawArcValue();
            }
        }

        private bool _centered = false;
        public bool Centered {
            get => _centered;
            set {
                _centered = value;
                DrawArcValue();
            }
        }

        private bool _enabled = true;
        public bool Enabled {
            get => _enabled;
            set {
                _enabled = value;
                DrawArcValue();
            }
        }

        private double _scale = 1;
        public double Scale {
            get => _scale;
            set {
                value = Math.Max(0, Math.Min(1, value));
                if (value != _value) {
                    _scale = value;

                    ArcCanvas.Width = width * _scale;
                    ArcCanvas.Height = height * _scale;

                    DrawArcBase();
                    DrawArcValue();
                }
            }
        }

        private string ValueString => $"{((_centered && RawValue > 0)? "+" : "")}{RawValue}{Unit}";

        private void DrawArc(Path Arc, double value, bool overrideBase) {
            double x_start = (radius * (Math.Cos((_centered && !overrideBase)? angle_center: angle_start) + 1) + strokeHalf) * _scale;
            double y_start = (radius * (-Math.Sin((_centered && !overrideBase)? angle_center: angle_start) + 1) + strokeHalf) * _scale;
            
            double angle_point = angle_start - Math.Abs(angle_end - angle_start) * value;

            double x_end = (radius * (Math.Cos(angle_point) + 1) + strokeHalf) * _scale;
            double y_end = (radius * (-Math.Sin(angle_point) + 1) + strokeHalf) * _scale;

            double angle = (((_centered && !overrideBase)? angle_center: angle_start) - angle_point) / Math.PI * 180;

            int large = Convert.ToInt32(angle > 180);
            int direction = Convert.ToInt32(angle > 0);

            Arc.StrokeThickness = stroke * _scale;
            if (!overrideBase) {
                Arc.Stroke = (IBrush)Application.Current.Styles.FindResource(Enabled? "ThemeAccentBrush" : "ThemeForegroundLowBrush");
                Display.Text = ValueString;
            }
            
            Arc.Data = Geometry.Parse(String.Format("M {0},{1} A {2},{2} {3} {4} {5} {6},{7}",
                x_start.ToString(CultureInfo.InvariantCulture),
                y_start.ToString(CultureInfo.InvariantCulture),
                (radius * _scale).ToString(CultureInfo.InvariantCulture),
                angle.ToString(CultureInfo.InvariantCulture),
                large,
                direction,
                x_end.ToString(CultureInfo.InvariantCulture),
                y_end.ToString(CultureInfo.InvariantCulture)
            ));
        }

        private void DrawArcBase() => DrawArc(ArcBase, 1, true);
        private void DrawArcValue() => DrawArc(Arc, _value, false);

        public HorizontalDial() {
            InitializeComponent();

            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            DrawArcBase();
        }

        private void LayoutChanged(object sender, EventArgs e) => DrawArcValue();

        private bool mouseHeld = false;
        private double lastY;

        private void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left) && Enabled) {
                mouseHeld = true;
                e.Device.Capture(ArcCanvas);

                lastY = e.GetPosition(ArcCanvas).Y;
                ArcCanvas.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            }
        }

        private void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton.HasFlag(MouseButton.Left)) {
                mouseHeld = false;
                e.Device.Capture(null);

                ArcCanvas.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        private void MouseMove(object sender, PointerEventArgs e) {
            if (mouseHeld && Enabled) {
                double Y = e.GetPosition(ArcCanvas).Y;
                Value += (lastY - Y) / 200;
                lastY = Y;
            }
        }
    }
}