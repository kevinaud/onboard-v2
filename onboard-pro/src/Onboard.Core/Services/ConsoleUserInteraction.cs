using Onboard.Core.Abstractions;

namespace Onboard.Core.Services;

/// <summary>
/// Concrete implementation of IUserInteraction using System.Console.
/// </summary>
public class ConsoleUserInteraction : IUserInteraction
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"═══ {message} ═══");
        Console.WriteLine();
        Console.ResetColor();
    }

    public void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    public void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    public void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    public string Prompt(string message)
    {
        Console.Write(message);
        return Console.ReadLine() ?? string.Empty;
    }
}
