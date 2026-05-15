using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using spacedefence_editor2.Models;

namespace spacedefence_editor2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public event EventHandler? RequestOpenFile;
    public event EventHandler? RequestSaveFileAs;

    private LevelData? _currentLevel;
    private string _statusText = "Redo";
    private string? _currentFilePath;
    private bool _isUnsaved;
    private bool _isPathDrawingMode;
    private ObservableCollection<Avalonia.Point> _pathDrawingPoints = new();

    public LevelData? CurrentLevel
    {
        get => _currentLevel;
        set => SetProperty(ref _currentLevel, value);
    }

    public bool IsPathDrawingMode
    {
        get => _isPathDrawingMode;
        set
        {
            if (SetProperty(ref _isPathDrawingMode, value))
            {
                if (!value) _pathDrawingPoints.Clear();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CurrentLevel)); // Trigger redraw to show/hide temp points
            }
        }
    }

    public ObservableCollection<Avalonia.Point> PathDrawingPoints => _pathDrawingPoints;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    public bool IsUnsaved
    {
        get => _isUnsaved;
        set => SetProperty(ref _isUnsaved, value);
    }

    private object? _selectedObject;

    public object? SelectedObject
    {
        get => _selectedObject;
        set => SetProperty(ref _selectedObject, value);
    }

    public MainWindowViewModel()
    {
        NewCommand = new RelayCommand(NewLevel);
        OpenCommand = new AsyncRelayCommand(OpenLevel);
        SaveCommand = new AsyncRelayCommand(SaveLevel);
        SaveAsCommand = new AsyncRelayCommand(SaveLevelAs);
        AddTrackBitCommand = new RelayCommand<Avalonia.Point?>(AddTrackBit);
        AddWaypointCommand = new RelayCommand<Avalonia.Point?>(AddWaypoint);
        TogglePathDrawingModeCommand = new RelayCommand(() => IsPathDrawingMode = !IsPathDrawingMode);
        AddPathDrawingPointCommand = new RelayCommand<Avalonia.Point?>(AddPathDrawingPoint);
        FinalizePathCommand = new RelayCommand(FinalizePath);
    }

    public ICommand NewCommand { get; }
    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand SaveAsCommand { get; }
    public IRelayCommand<Avalonia.Point?> AddTrackBitCommand { get; }
    public IRelayCommand<Avalonia.Point?> AddWaypointCommand { get; }
    public ICommand TogglePathDrawingModeCommand { get; }
    public IRelayCommand<Avalonia.Point?> AddPathDrawingPointCommand { get; }
    public ICommand FinalizePathCommand { get; }

    private void AddTrackBit(Avalonia.Point? pos)
    {
        if (CurrentLevel == null) return;
        int x = 0;
        int y = 0;
        if (pos.HasValue)
        {
            x = (int)(pos.Value.X / 60) + 1;
            y = (int)(pos.Value.Y / 60);
        }
        var newBit = new TrackBitData { X = x, Y = y, Direction = "Right" };
        CurrentLevel.Track.TrackBits.Add(newBit);
        SelectedObject = newBit;
        OnPropertyChanged(nameof(CurrentLevel));
        IsUnsaved = true;
    }

    private void AddWaypoint(Avalonia.Point? pos)
    {
        if (CurrentLevel == null) return;
        int x = 0;
        int y = 0;
        if (pos.HasValue)
        {
            x = (int)(pos.Value.X / 60) + 1;
            y = (int)(pos.Value.Y / 60);
        }
        var newPoint = new List<int> { x, y };
        CurrentLevel.Track.Path.Add(newPoint);
        SelectedObject = newPoint;
        OnPropertyChanged(nameof(CurrentLevel));
        IsUnsaved = true;
    }

    private void AddPathDrawingPoint(Avalonia.Point? pos)
    {
        if (!pos.HasValue) return;
        _pathDrawingPoints.Add(pos.Value);
        OnPropertyChanged(nameof(CurrentLevel)); // Redraw
        StatusText = $"Points: {_pathDrawingPoints.Count}";
    }

    private void FinalizePath()
    {
        if (CurrentLevel == null || _pathDrawingPoints.Count < 2)
        {
            IsPathDrawingMode = false;
            _pathDrawingPoints.Clear();
            return;
        }

        var waypoints = new List<System.Drawing.Point>();
        foreach (var p in _pathDrawingPoints)
        {
            waypoints.Add(new System.Drawing.Point((int)(p.X / 60) + 1, (int)(p.Y / 60)));
        }

        var fullPath = new List<System.Drawing.Point>();
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            var p1 = waypoints[i];
            var p2 = waypoints[i + 1];

            // Fill path between p1 and p2 (L-shaped path: horizontal then vertical)
            int stepX = p1.X < p2.X ? 1 : -1;
            for (int x = p1.X; x != p2.X; x += stepX)
            {
                var cell = new System.Drawing.Point(x, p1.Y);
                if (fullPath.Count == 0 || fullPath[fullPath.Count - 1] != cell)
                    fullPath.Add(cell);
            }

            int stepY = p1.Y < p2.Y ? 1 : -1;
            for (int y = p1.Y; y != p2.Y; y += stepY)
            {
                var cell = new System.Drawing.Point(p2.X, y);
                if (fullPath.Count == 0 || fullPath[fullPath.Count - 1] != cell)
                    fullPath.Add(cell);
            }
        }
        // Add the very last waypoint
        var lastWaypoint = waypoints[waypoints.Count - 1];
        if (fullPath.Count == 0 || fullPath[fullPath.Count - 1] != lastWaypoint)
            fullPath.Add(lastWaypoint);

        // Update LevelData Path
        CurrentLevel.Track.Path.Clear();
        foreach (var cell in fullPath)
        {
            CurrentLevel.Track.Path.Add(new List<int> { cell.X, cell.Y });
        }

        // Generate TrackBits
        CurrentLevel.Track.TrackBits.Clear();
        for (int i = 0; i < fullPath.Count; i++)
        {
            var current = fullPath[i];
            string direction = GetDirection(fullPath, i);

            CurrentLevel.Track.TrackBits.Add(new TrackBitData
            {
                X = current.X,
                Y = current.Y,
                Direction = direction
            });
        }

        IsPathDrawingMode = false;
        _pathDrawingPoints.Clear();
        IsUnsaved = true;
        OnPropertyChanged(nameof(CurrentLevel));
        StatusText = "Bana skapad med rätt format!";
    }

    private string GetDirection(List<System.Drawing.Point> path, int index)
    {
        var current = path[index];
        System.Drawing.Point? prev = index > 0 ? path[index - 1] : null;
        System.Drawing.Point? next = index < path.Count - 1 ? path[index + 1] : null;

        if (prev == null && next == null) return "Horizontal";

        if (prev == null)
        {
            // Start piece
            return current.X != next!.Value.X ? "Horizontal" : "Vertical";
        }

        if (next == null)
        {
            // End piece
            return current.X != prev!.Value.X ? "Horizontal" : "Vertical";
        }

        var p = prev.Value;
        var n = next.Value;

        // Same line
        if (p.X == current.X && n.X == current.X) return "Vertical";
        if (p.Y == current.Y && n.Y == current.Y) return "Horizontal";

        // Turns mapping:
        // Horizontal: Connects Left (X-1) and Right (X+1).
        // Vertical: Connects Top (Y-1) and Bottom (Y+1).
        // NE (North-East): Corner connecting Top (Y-1) and Right (X+1).
        // SE (South-East): Corner connecting Bottom (Y+1) and Right (X+1).
        // SW (South-West): Corner connecting Bottom (Y+1) and Left (X-1).
        // NW (North-West): Corner connecting Top (Y-1) and Left (X-1).

        bool hasTop = (p.Y == current.Y - 1 && p.X == current.X) || (n.Y == current.Y - 1 && n.X == current.X);
        bool hasBottom = (p.Y == current.Y + 1 && p.X == current.X) || (n.Y == current.Y + 1 && n.X == current.X);
        bool hasLeft = (p.X == current.X - 1 && p.Y == current.Y) || (n.X == current.X - 1 && n.Y == current.Y);
        bool hasRight = (p.X == current.X + 1 && p.Y == current.Y) || (n.X == current.X + 1 && n.Y == current.Y);

        if (hasTop && hasRight) return "NE";
        if (hasBottom && hasRight) return "SE";
        if (hasBottom && hasLeft) return "SW";
        if (hasTop && hasLeft) return "NW";

        return "Horizontal";
    }

    private bool HasTrackBitAt(int x, int y)
    {
        return CurrentLevel?.Track.TrackBits.Exists(b => b.X == x && b.Y == y) ?? false;
    }

    public void DeleteSelected()
    {
        if (CurrentLevel == null || SelectedObject == null) return;

        if (SelectedObject is TrackBitData bit)
        {
            CurrentLevel.Track.TrackBits.Remove(bit);
            SelectedObject = null;
            OnPropertyChanged(nameof(CurrentLevel));
            IsUnsaved = true;
        }
        else if (SelectedObject is List<int> point)
        {
            CurrentLevel.Track.Path.Remove(point);
            SelectedObject = null;
            OnPropertyChanged(nameof(CurrentLevel));
            IsUnsaved = true;
        }
    }

    private void NewLevel()
    {
        CurrentLevel = LevelService.CreateNewLevel();
        CurrentFilePath = null;
        IsUnsaved = false;
        StatusText = "Ny bana skapad";
    }

    private async Task OpenLevel()
    {
        RequestOpenFile?.Invoke(this, EventArgs.Empty);
    }

    public void LoadLevel(string path)
    {
        try
        {
            CurrentLevel = LevelService.LoadLevel(path);
            CurrentFilePath = path;
            IsUnsaved = false;
            StatusText = $"Laddade {path}";
        }
        catch (System.Exception ex)
        {
            StatusText = $"Fel vid laddning: {ex.Message}";
        }
    }

    private async Task SaveLevel()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            await SaveLevelAs();
            return;
        }

        if (CurrentLevel != null)
        {
            try
            {
                LevelService.SaveLevel(CurrentFilePath, CurrentLevel);
                IsUnsaved = false;
                StatusText = $"Sparade till {CurrentFilePath}";
            }
            catch (System.Exception ex)
            {
                StatusText = $"Fel vid sparning: {ex.Message}";
            }
        }
    }

    private async Task SaveLevelAs()
    {
        RequestSaveFileAs?.Invoke(this, EventArgs.Empty);
    }

    public void SaveLevelToPath(string path)
    {
        if (CurrentLevel != null)
        {
            try
            {
                LevelService.SaveLevel(path, CurrentLevel);
                CurrentFilePath = path;
                IsUnsaved = false;
                StatusText = $"Sparade till {path}";
            }
            catch (System.Exception ex)
            {
                StatusText = $"Fel vid sparning: {ex.Message}";
            }
        }
    }
}
