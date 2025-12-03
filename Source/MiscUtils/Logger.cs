using RimWorld;
using System;
using System.Linq;
using Verse;

public static class Logger
{
    public enum LogLevel
    {
        Debug = 0,
        Trace = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        Panic = 6
    }

    public static bool Enabled { get; set; } = true;
    public static LogLevel Level { get; set; } = LogLevel.Debug;

    private static void InternalLog(LogLevel severity, string message)
    {
        if (!Enabled || severity < Level)
        {
            return;
        }

        string prefix = $"[YAMP:{severity}]";
        switch (severity)
        {
            case LogLevel.Error:
            case LogLevel.Critical:
            case LogLevel.Panic:
                Verse.Log.Error($"{prefix}: {message}");
                break;
            case LogLevel.Warning:
                Verse.Log.Warning($"{prefix}: {message}");
                break;
            case LogLevel.Info:
            case LogLevel.Debug:
            case LogLevel.Trace:
            default:
                Verse.Log.Message($"{prefix}: {message}");
                break;
        }
    }

    public static void Log(string customHeader, string message)
    {
        InternalLog(LogLevel.Trace, $"[{customHeader}]: {message}");
    }

    public static void Log(LogLevel severity, string message)
    {
        InternalLog(severity, message);
    }

    public static void Trace(string message) => InternalLog(LogLevel.Trace, message);
    public static void Debug(string message) => InternalLog(LogLevel.Debug, message);
    public static void Info(string message) => InternalLog(LogLevel.Info, message);
    public static void Warning(string message) => InternalLog(LogLevel.Warning, message);
    public static void Error(string message) => InternalLog(LogLevel.Error, message);
    public static void Critical(string message) => InternalLog(LogLevel.Critical, message);
    public static void Panic(string message) => InternalLog(LogLevel.Panic, message);
}