﻿using System.Diagnostics.CodeAnalysis;

namespace Engine.Core.Logging;

public static partial class Logger
{
    private static readonly LogStream RecentLog = new(100);
    private static readonly PersistentLogStream PersistentLog = new();
    private static readonly Dictionary<LogLevel, LogStream> Logs = new();
    private static LogLevel _logLevel = LogLevel.Info;
    
    public static LogLevel RenderedLogLevel = LogLevel.Info;

    static Logger()
    {
        foreach (var level in System.Enum.GetValues<LogLevel>())
        {
            Logs[level] = new LogStream(300);
        }
    }

    public static void ReadLevel(LogLevel level, out List<Tuple<string, LogLevel>> log) => Logs[level].ReadAll(out log);
    public static void ReadRecent(out List<Tuple<string, LogLevel>> log) => RecentLog.ReadAll(out log, skipNonRecent: true);
    public static void ReadPersistent(out List<string> log) => PersistentLog.ReadAll(out log);
    
    public static void Debug(object message) => Write(message, null, LogLevel.Debug);
    public static void DebugNoNewline(object message) => Write(message, null, LogLevel.Debug, skipNewline: true);
    public static void Debug(object message, Exception e) => Write(message, e, LogLevel.Debug);
    public static void Debug(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Debug);
    public static void Log(object message) => Write(message, null, LogLevel.Log);
    public static void Log(object message, Exception e) => Write(message, e, LogLevel.Log);
    public static void LogF(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Log);
    public static void Info(object message) => Write(message, null, LogLevel.Info);
    public static void Info(object message, Exception e) => Write(message, e, LogLevel.Info);
    public static void InfoF(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Info);
    public static void Warn(object message) => Write(message, null, LogLevel.Warn);
    public static void Warn(object message, Exception e) => Write(message, e, LogLevel.Warn);
    public static void WarnF(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Warn);
    public static void Error(object message) => Write(message, null, LogLevel.Error);
    public static void Error(object message, Exception e) => Write(message, e, LogLevel.Error);
    public static void ErrorF(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Error);
    public static void Fatal(object message) => Write(message, null, LogLevel.Fatal);
    public static void Fatal(object message, Exception e) => Write(message, e, LogLevel.Fatal);
    public static void FatalF(string format, params object[] args) => Write(string.Format(format, args), null, LogLevel.Fatal);
    public static void ShowPersistent(object key, object? message) => PersistentLog.Write(key, message);
    public static void ShowPersistentF(object key, string format, params object[] args) => PersistentLog.Write(key, string.Format(format, args));
    public static void ClearPersistent(object key) => PersistentLog.Clear(key);
    
    private static void Write(object message, Exception? exception, LogLevel level, bool skipNewline = false)
    {
        var str = message.ToString()!;
        Logs[level].Write(str, level);
        if (skipNewline)
            Console.Write(str, exception);
        else
            Console.WriteLine(str, exception);
        Console.Write(exception);
        
        if (level < _logLevel)
            return;

        RecentLog.Write(str, level);
    }

    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    private partial class LogStream
    {
        private int _index = 0;
        private readonly int _capacity;

        private int _logLength = 0;
        private readonly List<LogEntry> _log;
        private readonly List<Tuple<string, LogLevel>> _orderedLog;
        private DateTime _lastRead = DateTime.MinValue;

        public LogStream(int capacity)
        {
            _capacity = capacity;
            _log = new List<LogEntry>(capacity);
            _orderedLog = new List<Tuple<string, LogLevel>>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                _log.Add(new LogEntry());
                _orderedLog.Add(new Tuple<string, LogLevel>(string.Empty, LogLevel.Debug));
            }
        }

        internal void Write(string message, LogLevel level)
        {
            var previousIndex = _index - 1;
            if (previousIndex < 0)
                previousIndex = _capacity - 1;

            if (_log[previousIndex].Message == message && _log[previousIndex].Level == level)
            {
                _log[previousIndex].Timestamp = DateTime.Now;
                _log[previousIndex].EventCount += 1;
                return;
            }
            _log[_index].Timestamp = DateTime.Now;
            _log[_index].Level = level;
            _log[_index].Message = message;
            _index += 1;
            if (_index >= _capacity - 1)
            {
                _index = 0;
            }

            if (_logLength < _capacity)
                _logLength += 1;
        }
        
        internal void ReadAll(out List<Tuple<string, LogLevel>> orderedLog, bool skipNonRecent = false)
        {
            if (DateTime.Now - _lastRead < TimeSpan.FromSeconds(0.15))
            {
                orderedLog = _orderedLog;
                return;
            }
            
            _orderedLog.Clear();
            for (var i = 0; i < _logLength; i++)
            {
                var indexToRead = (_index + i) % _logLength;
                var entry = _log[indexToRead];
                if (skipNonRecent && entry.Timestamp < DateTime.Now.AddSeconds(-5))
                    continue;
                var formattedEntry = $@"{entry.Timestamp:hh\:mm\:ss} - {entry.Message}";
                if (entry.EventCount > 1)
                    formattedEntry += $" (x{entry.EventCount})";
                _orderedLog.Add(new Tuple<string, LogLevel>(formattedEntry, entry.Level));
            }

            _lastRead = DateTime.Now;
            _orderedLog.Reverse();
            orderedLog = _orderedLog;
        }
    }

    private class LogEntry
    {
        internal DateTime Timestamp { get; set; } = DateTime.MinValue;
        internal LogLevel Level { get; set; } = LogLevel.Debug;
        internal string Message { get; set; } = string.Empty;
        internal int EventCount { get; set; } = 0;
    }

    private class PersistentLogStream
    {
        private readonly Dictionary<string, string> _log = new();
        
        internal void Write(object key, object? message)
        {
            if (message is null)
                return;
            _log[key.ToString()!] = message.ToString()!;
        }
        
        internal void Clear(object key)
        {
            _log.Remove(key.ToString()!);
        }
        
        internal void ReadAll(out List<string> orderedLog)
        {
            orderedLog = new List<string>(_log.Count);
            foreach (var kvp in _log)
            {
                orderedLog.Add(kvp.Value);
            }
        }
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal,
    Log
}