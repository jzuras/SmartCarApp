using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics;

namespace SmartCarWebApp;

static public class TempDataExtension
{
    const string ErrorResultKey = "ErrorResult";
    const string NoVehiclesKey = "NoVehicles";

    public static void ErrorResult(this ITempDataDictionary tempData, string message)
    {
        tempData[ErrorResultKey] = message;
    }

    public static void ErrorResultAppend(this ITempDataDictionary tempData, string message, string separator)
    {
        var current = tempData[ErrorResultKey] as string;
        if (string.IsNullOrEmpty(current) == false)
        {
            message = current + separator + message;
        }

        tempData[ErrorResultKey] = message;
    }

    public static string ErrorResult(this ITempDataDictionary tempData)
    {
        var returnValue = tempData[ErrorResultKey] as string;

        if (returnValue == null)
        {
            return "";
        }
        return returnValue as string;
    }

    public static void NoVehicles(this ITempDataDictionary tempData, string message)
    {
        tempData[NoVehiclesKey] = message;
    }

    public static string NoVehicles(this ITempDataDictionary tempData)
    {
        var returnValue = tempData[NoVehiclesKey] as string;

        if (returnValue == null)
        {
            return "";
        }
        return returnValue as string;
    }
}

public static class ConsoleExtensions
{
    /// <summary>
    /// Writes green text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleGreen(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Green);
    }

    /// <summary>
    /// Writes red text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleRed(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Red);
    }

    /// <summary>
    /// Writes yellow text to the console.
    /// </summary>
    /// <param name="text">The text.</param>
    [DebuggerStepThrough]
    public static void ConsoleYellow(this string text)
    {
        text.ColoredWriteLine(ConsoleColor.Yellow);
    }

    /// <summary>
    /// Writes out text with the specified ConsoleColor.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="color">The color.</param>
    [DebuggerStepThrough]
    public static void ColoredWriteLine(this string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

#region Logger Extensions Using Color If Console
public static class LoggingExtensions
{
    public static bool UseColoring = false;

    #region LogInformation
    public static void LogInformationExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Blue, LogLevel.Information, null, null, args);
    }

    public static void LogInformationExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Blue, LogLevel.Information, eventId, null, args);
    }

    public static void LogInformationExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Blue, LogLevel.Information, null, exception, args);
    }

    public static void LogInformationExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Blue, LogLevel.Information, eventId, exception, args);
    }
    #endregion

    #region LogWarning

    public static void LogWarningExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Yellow, LogLevel.Warning, null, null, args);
    }

    public static void LogWarningExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Yellow, LogLevel.Warning, eventId, null, args);
    }

    public static void LogWarningExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Yellow, LogLevel.Warning, null, exception, args);
    }

    public static void LogWarningExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Yellow, LogLevel.Warning, eventId, exception, args);
    }
    #endregion

    #region LogError

    public static void LogErrorExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Error, null, null, args);
    }

    public static void LogErrorExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Error, eventId, null, args);
    }

    public static void LogErrorExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Error, null, exception, args);
    }

    public static void LogErrorExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Error, eventId, exception, args);
    }
    #endregion

    #region LogCritical
    public static void LogCriticalExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Critical, null, null, args);
    }

    public static void LogCriticalExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Critical, eventId, null, args);
    }

    public static void LogCriticalExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Critical, null, exception, args);
    }

    public static void LogCriticalExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Red, LogLevel.Critical, eventId, exception, args);
    }
    #endregion

    #region LogDebug
    public static void LogDebugExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Green, LogLevel.Debug, null, null, args);
    }

    public static void LogDebugExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Green, LogLevel.Debug, eventId, null, args);
    }

    public static void LogDebugExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Green, LogLevel.Debug, null, exception, args);
    }

    public static void LogDebugExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Green, LogLevel.Debug, eventId, exception, args);
    }
    #endregion

    #region LogTrace
    public static void LogTraceExt<T>(this ILogger<T> logger, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.Cyan, LogLevel.Trace, null, null, args);
    }

    public static void LogTraceExt<T>(this ILogger<T> logger, EventId eventId, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.White, LogLevel.Trace, eventId, null, args);
    }

    public static void LogTraceExt<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.White, LogLevel.Trace, null, exception, args);
    }

    public static void LogTraceExt<T>(this ILogger<T> logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        LogColored(logger, message, ConsoleColor.White, LogLevel.Trace, eventId, exception, args);
    }
    #endregion

    #region Helper Methods
    //private static void LogColored(ILogger logger, string message, ConsoleColor color, LogLevel logLevel, EventId? eventId = null, Exception? exception = null, params object[] args)
    private static void LogColored<T>(ILogger<T> logger, string message, ConsoleColor color, LogLevel logLevel, EventId? eventId = null, Exception? exception = null, params object[] args)
    {
        if (UseColoring)
        {
            Console.ForegroundColor = color;
            WriteShortLogLevel(logLevel); // writes "crit", possibly in diff fg color
            //var shortLogLevel = GetShortLogLevel(logLevel);
            var categoryName = typeof(T).FullName;
            Console.WriteLine($": {categoryName}[{eventId ?? 0}]");
            //Console.WriteLine($"{shortLogLevel}: {categoryName}[{eventId ?? 0}]");
            Console.WriteLine($"      {message}");
            if(exception != null)
            {
                Console.WriteLine($"      {exception.GetType().FullName}: {exception.Message}");
            }
            Console.ResetColor();
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(eventId ?? default, exception, message, args);
                break;
            case LogLevel.Debug:
                logger.LogDebug(eventId ?? default, exception, message, args);
                break;
            case LogLevel.Information:
                logger.LogInformation(eventId ?? default, exception, message, args);
                break;
            case LogLevel.Warning:
                logger.LogWarning(eventId ?? default, exception, message, args);
                break;
            case LogLevel.Error:
                logger.LogError(eventId ?? default, exception, message, args);
                break;
            case LogLevel.Critical:
                logger.LogCritical(eventId ?? default, exception, message, args);
                break;
        }
    }

    private static void WriteShortLogLevel(LogLevel logLevel)
    {
        var shortLogLevel = GetShortLogLevel(logLevel);
        if (logLevel == LogLevel.Error)
        {
            var saveFGColor = Console.ForegroundColor;
            var saveBGColor = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write($"{shortLogLevel}");
            Console.ForegroundColor = saveFGColor;
            Console.BackgroundColor = saveBGColor;
        }
        else if (logLevel == LogLevel.Critical)
        {
            var saveFGColor = Console.ForegroundColor;
            var saveBGColor = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write($"{shortLogLevel}");
            Console.ForegroundColor = saveFGColor;
            Console.BackgroundColor = saveBGColor;
        }
        else
        {
            Console.Write($"{shortLogLevel}");
        }
    }

    private static string GetShortLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "unkn"
        };
    }
    #endregion
}
#endregion
