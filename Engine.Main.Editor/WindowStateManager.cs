﻿using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Text.Json;
using Engine.Core.Logging;
using Engine.Hypervisor.Editor.Windowing;
using Timer = System.Timers.Timer;

namespace Engine.Main.Editor;

/// <summary>
/// Handles saving and loading window state (position, size, monitor)
/// with a short-lived persistence (within 1 minute by default)
/// </summary>
public static class WindowStateManager
{
    private const string WindowStateFilename = "window_state.json";
    private const int RecentTimeThresholdMinutes = 30;
    private const int AutosaveIntervalMs = 5000;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {WriteIndented = true };
    
    // The full path to save the window state
    private static readonly string WindowStateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CustomEngine/Editor",
        WindowStateFilename
    );

    private static Timer? _autoSaveTimer;
    private static IWindow? _currentWindow;
    private static WindowState _lastSavedState = null!;
    private static bool _isDirty;

    private class WindowState
    {
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public DateTime LastSaved { get; set; }
    }
    
    public static void SaveWindowState(IWindow window)
    {
        var directory = Path.GetDirectoryName(WindowStateFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        if (window.Monitor == null)
            return;
        

        if (_isDirty)
        {
            var windowScale = window.Monitor?.Index == 0 ? Vector2D<float>.One : GarbageFixes.GetPrimaryMonitorScale();
            var scaledSize = new Vector2D<int>(
                (int)(window.FramebufferSize.X * windowScale.X),
                (int)(window.FramebufferSize.Y * windowScale.Y)
            );
            
            Logger.Info("Saving window state: " +
                       $"Position=({window.Position.X}, {window.Position.Y}), " +
                       $"Size=({scaledSize.X}, {scaledSize.Y})");
            _lastSavedState.PositionX = window.Position.X;
            _lastSavedState.PositionY = window.Position.Y;
            _lastSavedState.SizeX = scaledSize.X;
            _lastSavedState.SizeY = scaledSize.Y;
        }
        _lastSavedState.LastSaved = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(_lastSavedState, JsonSerializerOptions);
        File.WriteAllText(WindowStateFilePath, json);
        _isDirty = false;
    }
    
    public static bool TryLoadWindowState(ref WindowOptions opts)
    {
        _lastSavedState = new WindowState
        {
            PositionX = opts.Position.X,
            PositionY = opts.Position.Y,
            SizeX = opts.Size.X,
            SizeY = opts.Size.Y,
            LastSaved = DateTime.UtcNow
        };
        
        if (!File.Exists(WindowStateFilePath))
            return false;
            
        try
        {
            var json = File.ReadAllText(WindowStateFilePath);
            var state = JsonSerializer.Deserialize<WindowState>(json);
            
            // Only return the state if it was saved recently
            if (state == null || (DateTime.UtcNow - state.LastSaved).TotalMinutes > RecentTimeThresholdMinutes)
                return false;
            
            _lastSavedState.PositionX = state.PositionX;
            _lastSavedState.PositionY = state.PositionY;
            _lastSavedState.SizeX = state.SizeX;
            _lastSavedState.SizeY = state.SizeY;

            if (state.SizeX <= 0 || state.SizeY <= 0)
                return false;
            
            opts = opts with
            {
                Position = new Vector2D<int>(state.PositionX, state.PositionY),
                Size = new Vector2D<int>(state.SizeX, state.SizeY)
            };
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static void SetupAutosaveHandler(IWindow window)
    {
        _currentWindow = window;
        
        // Set up window event listeners
        window.Move += _ => _isDirty = true;
        window.Resize += _ => _isDirty = true;
        
        // Create and start the autosave timer
        _autoSaveTimer = new Timer(AutosaveIntervalMs);
        _autoSaveTimer.Elapsed += (_, _) =>
        {
            if (_currentWindow == null) return;
            SaveWindowState(_currentWindow);
        };
        
        _autoSaveTimer.AutoReset = true;
        _autoSaveTimer.Start();
    }
    
    public static void Cleanup()
    {
        if (_autoSaveTimer != null)
        {
            _autoSaveTimer.Stop();
            _autoSaveTimer.Dispose();
            _autoSaveTimer = null;
        }
        
        _currentWindow = null;
    }
}

