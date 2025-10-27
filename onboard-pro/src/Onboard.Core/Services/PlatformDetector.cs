using System.Runtime.InteropServices;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;

namespace Onboard.Core.Services;

/// <summary>
/// Concrete implementation of IPlatformDetector using System.Runtime.InteropServices.RuntimeInformation.
/// </summary>
public class PlatformDetector : IPlatformDetector
{
    public PlatformFacts Detect()
    {
        var os = DetectOperatingSystem();
        var arch = DetectArchitecture();
        var isWsl = DetectWsl();
        var homeDir = DetectHomeDirectory();

        return new PlatformFacts(os, arch, isWsl, homeDir);
    }

    private static Models.OperatingSystem DetectOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Models.OperatingSystem.Windows;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Models.OperatingSystem.MacOs;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Models.OperatingSystem.Linux;

        return Models.OperatingSystem.Unknown;
    }

    private static Models.Architecture DetectArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => Models.Architecture.X64,
            System.Runtime.InteropServices.Architecture.Arm64 => Models.Architecture.Arm64,
            _ => Models.Architecture.Unknown
        };
    }

    private static bool DetectWsl()
    {
        // Check for WSL-specific environment variables
        var wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
        var wslInterop = Environment.GetEnvironmentVariable("WSL_INTEROP");
        
        return !string.IsNullOrEmpty(wslDistro) || !string.IsNullOrEmpty(wslInterop);
    }

    private static string DetectHomeDirectory()
    {
        // On Windows, use USERPROFILE; on Unix-like systems, use HOME
        return Environment.GetEnvironmentVariable("HOME") 
               ?? Environment.GetEnvironmentVariable("USERPROFILE") 
               ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
