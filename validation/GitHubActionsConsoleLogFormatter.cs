using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace AdvocateValidation;

public class GitHubActionsConsoleLogFormatter() : ConsoleFormatter(nameof(GitHubActionsConsoleLogFormatter))
{
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string filePath = string.Empty;
        scopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                filePath = values.FirstOrDefault(x => x.Key == "filePath").Value?.ToString() ?? string.Empty;
            }
        }, textWriter);

        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        CreateDefaultLogMessage(textWriter, logEntry, message, filePath);
    }

    private static void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, string filePath)
    {
        Exception? exception = logEntry.Exception;

        textWriter.Write($"::{LogLevelToGitHubActionsLogLevel(logEntry.LogLevel)} file={filePath}:: ");

        WriteMessage(textWriter, message);

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            WriteMessage(textWriter, exception.ToString());
        }

        textWriter.Write(Environment.NewLine);
    }

    private static void WriteMessage(TextWriter textWriter, string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            textWriter.Write(message);
            textWriter.Write(Environment.NewLine);
        }
    }

    private static string LogLevelToGitHubActionsLogLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "debug",
        LogLevel.Debug => "debug",
        LogLevel.Information => "notice",
        LogLevel.Warning => "warning",
        LogLevel.Error => "error",
        LogLevel.Critical => "error",
        _ => "debug",
    };
}

public class GitHubActionsConsoleLogFormatterOptions : ConsoleFormatterOptions
{
}
