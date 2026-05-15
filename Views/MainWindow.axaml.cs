using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using spacedefence_editor2.Models;
using spacedefence_editor2.ViewModels;

namespace spacedefence_editor2.Views;

public partial class MainWindow : Window
{
    private Avalonia.Point? _lastClickPosition;

    public MainWindow()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(MainWindowViewModel.CurrentLevel) || 
                        args.PropertyName == nameof(MainWindowViewModel.SelectedObject))
                    {
                        RenderLevel();
                    }
                };

                // Add pointer move logic for dragging waypoints/trackbits
                var canvas = this.FindControl<Canvas>("EditorCanvas");
                if (canvas != null)
                {
                    canvas.PointerMoved += (s, e) =>
                    {
                        var pointer = e.GetCurrentPoint(canvas);
                        if (pointer.Properties.IsLeftButtonPressed && vm.SelectedObject != null)
                        {
                            var pos = pointer.Position;
                            int cellSize = 60;
                            int gridX = (int)(pos.X / cellSize);
                            int gridY = (int)(pos.Y / cellSize);

                            if (vm.SelectedObject is Models.TrackBitData bit)
                            {
                                if (bit.X != gridX || bit.Y != gridY)
                                {
                                    bit.X = gridX;
                                    bit.Y = gridY;
                                    RenderLevel();
                                }
                            }
                            else if (vm.SelectedObject is List<int> point)
                            {
                                if (point[0] != gridX || point[1] != gridY)
                                {
                                    point[0] = gridX;
                                    point[1] = gridY;
                                    RenderLevel();
                                }
                            }
                        }
                    };
                }
                
                // Register for dialog requests from ViewModel
                vm.RequestOpenFile += async (sender, args) => await OpenFileDialog();
                vm.RequestSaveFileAs += async (sender, args) => await SaveAsFileDialog();
            }
        };

        var canvas = this.FindControl<Canvas>("EditorCanvas");
        if (canvas != null)
        {
            canvas.PointerPressed += (s, e) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    var pointer = e.GetCurrentPoint(canvas);
                    if (pointer.Properties.IsRightButtonPressed)
                    {
                        // Store the click position for context menu commands
                        _lastClickPosition = pointer.Position;
                        if (canvas.ContextMenu != null)
                        {
                            canvas.ContextMenu.Tag = _lastClickPosition;
                        }
                    }
                    else
                    {
                        if (vm.IsPathDrawingMode)
                        {
                            vm.AddPathDrawingPointCommand.Execute(pointer.Position);
                        }
                        else
                        {
                            vm.SelectedObject = null;
                        }
                    }
                }
            };
        }
        this.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Delete)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.DeleteSelected();
                }
            }
        };
    }

    private void RenderLevel()
    {
        var canvas = this.FindControl<Canvas>("EditorCanvas");
        if (canvas == null) return;

        canvas.Children.Clear();

        if (DataContext is not MainWindowViewModel vm || vm.CurrentLevel == null)
            return;

        var level = vm.CurrentLevel;
        const int cellSize = 60;
        
        canvas.Width = level.Track.Width * cellSize;
        canvas.Height = level.Track.Height * cellSize;

        // Render Wave preview (first wave for now)
        // Moved here to be below track/path
        if (level.Waves.Count > 0)
        {
            var firstWave = level.Waves[0];
            foreach (var alien in firstWave.Aliens)
            {
                var ellipse = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = GetColor(alien.Color),
                    Stroke = Avalonia.Media.Brushes.White,
                    StrokeThickness = 1,
                    Tag = alien,
                    Opacity = 0.7,
                    ZIndex = 20
                };
                // Place it at the start of the path for now
                if (level.Track.Path.Count > 0)
                {
                    var start = level.Track.Path[0];
                    Canvas.SetLeft(ellipse, start[0] * cellSize + cellSize / 4);
                    Canvas.SetTop(ellipse, start[1] * cellSize + cellSize / 4);
                }
                canvas.Children.Add(ellipse);
            }
        }

        var themeBrush = DrawingUtils.GetBrushByName(level.Track.ThemeColor);
        var gridBrush = DrawingUtils.GetBrushByName(level.Track.ThemeColor, 0.35);

        // Render Grid
        for (int x = 0; x <= level.Track.Width; x++)
        {
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(x * cellSize, 0),
                EndPoint = new Avalonia.Point(x * cellSize, level.Track.Height * cellSize),
                Stroke = gridBrush,
                StrokeThickness = 0.5,
                ZIndex = 0
            };
            line.PointerPressed += (s, e) => { vm.SelectedObject = null; RenderLevel(); };
            canvas.Children.Add(line);
        }
        for (int y = 0; y <= level.Track.Height; y++)
        {
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(0, y * cellSize),
                EndPoint = new Avalonia.Point(level.Track.Width * cellSize, y * cellSize),
                Stroke = gridBrush,
                StrokeThickness = 0.5,
                ZIndex = 0
            };
            line.PointerPressed += (s, e) => { vm.SelectedObject = null; RenderLevel(); };
            canvas.Children.Add(line);
        }

        // Render TrackBits
        foreach (var bit in level.Track.TrackBits)
        {
            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = cellSize - 4,
                Height = cellSize - 4,
                Stroke = bit == vm.SelectedObject ? Avalonia.Media.Brushes.Yellow : themeBrush,
                StrokeThickness = bit == vm.SelectedObject ? 4 : 2,
                Fill = Avalonia.Media.Brushes.Transparent,
                Tag = bit,
                ZIndex = 1
            };
            rect.PointerPressed += (s, e) =>
            {
                vm.SelectedObject = bit;
                RenderLevel();
                e.Handled = true;
            };
            Canvas.SetLeft(rect, bit.X * cellSize + 2);
            Canvas.SetTop(rect, bit.Y * cellSize + 2);
            canvas.Children.Add(rect);

            var text = new TextBlock
            {
                Text = bit.Direction,
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                ZIndex = 2
            };
            Canvas.SetLeft(text, bit.X * cellSize + 5);
            Canvas.SetTop(text, bit.Y * cellSize + 5);
            canvas.Children.Add(text);
        }

        // Render Path
        if (level.Track.Path.Count > 0)
        {
            for (int i = 0; i < level.Track.Path.Count; i++)
            {
                var point = level.Track.Path[i];

                var pathCellRect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = cellSize - 4,
                    Height = cellSize - 4,
                    Stroke = themeBrush,
                    StrokeThickness = 2,
                    Fill = themeBrush,
                    Opacity = 0.2,
                    ZIndex = 0
                };
                Canvas.SetLeft(pathCellRect, point[0] * cellSize + 2);
                Canvas.SetTop(pathCellRect, point[1] * cellSize + 2);
                canvas.Children.Add(pathCellRect);

                var ellipse = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = point == vm.SelectedObject ? Avalonia.Media.Brushes.Yellow : Avalonia.Media.Brushes.Orange,
                    Stroke = Avalonia.Media.Brushes.Black,
                    StrokeThickness = 1,
                    Tag = point,
                    ZIndex = 10
                };
                
                ellipse.PointerPressed += (s, e) =>
                {
                    vm.SelectedObject = point;
                    RenderLevel();
                    e.Handled = true;
                };

                Canvas.SetLeft(ellipse, point[0] * cellSize + cellSize / 2 - 8);
                Canvas.SetTop(ellipse, point[1] * cellSize + cellSize / 2 - 8);
                canvas.Children.Add(ellipse);

                if (i < level.Track.Path.Count - 1)
                {
                    var p1 = level.Track.Path[i];
                    var p2 = level.Track.Path[i + 1];

                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = new Avalonia.Point(p1[0] * cellSize + cellSize / 2, p1[1] * cellSize + cellSize / 2),
                        EndPoint = new Avalonia.Point(p2[0] * cellSize + cellSize / 2, p2[1] * cellSize + cellSize / 2),
                        Stroke = themeBrush,
                        StrokeThickness = 3,
                        Opacity = 0.5,
                        ZIndex = 5
                    };
                    canvas.Children.Add(line);
                }
            }
        }

        // Render Path Drawing Points (Temporary)
        if (vm.IsPathDrawingMode)
        {
            for (int i = 0; i < vm.PathDrawingPoints.Count; i++)
            {
                var p = vm.PathDrawingPoints[i];
                var dot = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Avalonia.Media.Brushes.Red,
                    ZIndex = 50
                };
                Canvas.SetLeft(dot, p.X - 5);
                Canvas.SetTop(dot, p.Y - 5);
                canvas.Children.Add(dot);

                if (i > 0)
                {
                    var prev = vm.PathDrawingPoints[i - 1];
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = prev,
                        EndPoint = p,
                        Stroke = Avalonia.Media.Brushes.Red,
                        StrokeThickness = 2,
                        Opacity = 0.8,
                        ZIndex = 45
                    };
                    canvas.Children.Add(line);
                }
            }
        }
    }

    private Avalonia.Media.IBrush GetColor(string colorName)
    {
        return DrawingUtils.GetBrushByName(colorName);
    }

    private async System.Threading.Tasks.Task OpenFileDialog()
    {
        var storage = this.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Level File",
            FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadLevel(path);
            }
        }
    }

    private async System.Threading.Tasks.Task SaveAsFileDialog()
    {
        var storage = this.StorageProvider;
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Level File",
            FileTypeChoices = new List<FilePickerFileType> { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } },
            DefaultExtension = "json"
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SaveLevelToPath(path);
            }
        }
    }
}