namespace Onboard.Core.Tests.TestDoubles;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

internal sealed class InMemoryLogger<TCategory> : ILogger<TCategory>
{
    private static readonly NullScope Scope = new();

    public List<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return Scope;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
