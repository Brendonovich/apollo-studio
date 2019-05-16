﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl, ISelectViewer {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ChainInfoEventHandler(int index);
        public event ChainInfoEventHandler ChainAdded;
        public event ChainInfoEventHandler ChainRemoved;

        public delegate void ChainExpandedEventHandler(int? index);
        public event ChainExpandedEventHandler ChainExpanded;

        Chain _chain;
        bool selected = false;

        Grid Root;
        TextBlock NameText;
        public VerticalAdd ChainAdd;

        Grid Draggable;
        ContextMenu ChainContextMenu;
        TextBox Input;

        private void UpdateText() => UpdateText(_chain.ParentIndex.Value, _chain.Name);
        private void UpdateText(int index) => UpdateText(index, _chain.Name);
        private void UpdateText(string name) => UpdateText(_chain.ParentIndex.Value, name);
        private void UpdateText(int index, string name) => NameText.Text = name.Replace("#", (index + 1).ToString());
        
        private void ApplyHeaderBrush(IBrush brush) {
            if (IsArrangeValid) Root.Background = brush;
            else this.Resources["BackgroundBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush((IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush2"));
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush(new SolidColorBrush(Color.Parse("Transparent")));
            selected = false;
        }

        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;

            Root = this.Get<Grid>("DropZone");
            Deselect();

            NameText = this.Get<TextBlock>("Name");
            UpdateText();
            _chain.ParentIndexChanged += UpdateText;
            _chain.NameChanged += UpdateText;

            ChainAdd = this.Get<VerticalAdd>("DropZoneAfter");

            ChainContextMenu = (ContextMenu)this.Resources["ChainContextMenu"];
            ChainContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));

            Draggable = this.Get<Grid>("Draggable");
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Input = this.Get<TextBox>("Input");
            Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);
        }

        private void Chain_Action(string action) => Track.Get(_chain).Window?.Selection.Action(action, (ISelectParent)_chain.Parent, _chain.ParentIndex.Value);

        private void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track.Get(_chain).Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        private void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                Track.Get(_chain).Window?.Selection.Select(_chain, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("chain", Track.Get(_chain).Window?.Selection.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Left)
                    ChainExpanded?.Invoke(_chain.ParentIndex.Value);
                
                if (e.MouseButton == MouseButton.Right)
                    ChainContextMenu.Open(Draggable);
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain") && !e.Data.Contains("device")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter")
                source = source.Parent;

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);
            bool result;

            if (e.Data.Contains("chain")) {
                List<Chain> moving = ((List<ISelect>)e.Data.Get("chain")).Select(i => (Chain)i).ToList();
            
                if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) {
                    if (_chain.ParentIndex == 0) result = Chain.Move(moving, (IMultipleChainParent)_chain.Parent, copy);
                    else result = Chain.Move(moving, ((IMultipleChainParent)_chain.Parent)[_chain.ParentIndex.Value - 1], copy);
                } else result = Chain.Move(moving, _chain, copy);

            } else if (e.Data.Contains("device")) {
                List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();
            
                if (source.Name == "DropZone") {
                    ((IMultipleChainParent)_chain.Parent).SpecificViewer.Expand(_chain.ParentIndex);
                    
                    if (_chain.Count > 0) result = Device.Move(moving, _chain.Devices.Last(), copy);
                    else result = Device.Move(moving, _chain, copy);
                } else {
                    Chain_Add();
                    result = Device.Move(moving, ((IMultipleChainParent)_chain.Parent)[_chain.ParentIndex.Value + 1], copy);
                }

            } else return;

            if (!result) e.DragEffects = DragDropEffects.None;
        }
        
        private void Chain_Add() => ChainAdded?.Invoke(_chain.ParentIndex.Value + 1);
        private void Chain_Remove() => ChainRemoved?.Invoke(_chain.ParentIndex.Value);

        int Input_Left, Input_Right;
        List<string> Input_Clean;
        bool Input_Ignore = false;

        private void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            if (Input_Ignore) return;

            Input_Ignore = true;
            for (int i = Input_Left; i <= Input_Right; i++)
                ((IMultipleChainParent)_chain.Parent)[i].Name = text;
            Input_Ignore = false;
        }

        public void StartInput(int left, int right) {
            Input_Left = left;
            Input_Right = right;

            Input_Clean = new List<string>();
            for (int i = left; i <= right; i++)
                Input_Clean.Add(((IMultipleChainParent)_chain.Parent)[i].Name);

            Input.Text = _chain.Name;
            Input.SelectionStart = 0;
            Input.SelectionEnd = Input.Text.Length;

            Input.Opacity = 1;
            Input.IsHitTestVisible = true;
            Input.Focus();
        }
        
        private void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = _chain.Name;

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            List<string> r = (from i in Enumerable.Range(0, Input_Clean.Count) select Input.Text).ToList();

            if (!r.SequenceEqual(Input_Clean)) {
                int left = Input_Left;
                int right = Input_Right;
                List<string> u = (from i in Input_Clean select i).ToList();

                Program.Project.Undo.Add($"Chain Renamed", () => {
                    for (int i = left; i <= right; i++)
                        ((IMultipleChainParent)_chain.Parent)[i].Name = u[i - left];
                    
                    Track.Get(_chain).Window?.Selection.Select(((IMultipleChainParent)_chain.Parent)[left]);
                    Track.Get(_chain).Window?.Selection.Select(((IMultipleChainParent)_chain.Parent)[right], true);
                    
                }, () => {
                    for (int i = left; i <= right; i++)
                        ((IMultipleChainParent)_chain.Parent)[i].Name = r[i - left];
                    
                    Track.Get(_chain).Window?.Selection.Select(((IMultipleChainParent)_chain.Parent)[left]);
                    Track.Get(_chain).Window?.Selection.Select(((IMultipleChainParent)_chain.Parent)[right], true);
                });
            }
        }

        public void SetName(string name) {
            if (Input_Ignore) return;

            Input_Ignore = true;
            Input.Text = name;
            Input_Ignore = false;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return)
                this.Focus();

            e.Handled = true;
        }

        private void Input_KeyUp(object sender, KeyEventArgs e) => e.Handled = true;

        private void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
