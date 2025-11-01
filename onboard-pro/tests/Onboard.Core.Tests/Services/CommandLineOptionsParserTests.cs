namespace Onboard.Core.Tests.Services;

using Onboard.Core.Services;

[TestFixture]
public class CommandLineOptionsParserTests
{
  [Test]
  public void TryParse_WhenArgsIsNull_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => CommandLineOptionsParser.TryParse(null!, out _, out _));
  }

  [Test]
  public void TryParse_WhenNoOptionsSpecified_ReturnsDefaults()
  {
    bool success = CommandLineOptionsParser.TryParse(Array.Empty<string>(), out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsWslGuestMode, Is.False);
      Assert.That(options.IsDryRun, Is.False);
      Assert.That(options.IsVerbose, Is.False);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithSeparateModeArguments_SetsWslGuestMode()
  {
    bool success = CommandLineOptionsParser.TryParse(
      new[] { "--mode", "wsl-guest" },
      out var options,
      out string? error
    );

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsWslGuestMode, Is.True);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithEqualsSyntax_SetsWslGuestMode()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--mode=wsl-guest" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsWslGuestMode, Is.True);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithMissingModeValue_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--mode" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --mode option requires a value."));
    });
  }

  [Test]
  public void TryParse_WithEmptyModeValue_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--mode=" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --mode option requires a non-empty value."));
    });
  }

  [Test]
  public void TryParse_WithInvalidModeValue_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--mode", "invalid" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("Unsupported --mode value 'invalid'. Expected 'wsl-guest'."));
    });
  }

  [Test]
  public void TryParse_WithDuplicateModeOption_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(
      new[] { "--mode=wsl-guest", "--mode=wsl-guest" },
      out _,
      out string? error
    );

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --mode option can only be specified once."));
    });
  }

  [Test]
  public void TryParse_WithDryRunFlag_SetsDryRun()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--dry-run" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsDryRun, Is.True);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithDryRunEqualsFalse_AcceptsValue()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--dry-run=false" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsDryRun, Is.False);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithInvalidDryRunValue_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--dry-run=maybe" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("Unsupported --dry-run value 'maybe'. Expected 'true' or 'false'."));
    });
  }

  [Test]
  public void TryParse_WithDuplicateDryRunFlag_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--dry-run", "--dry-run" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --dry-run option can only be specified once."));
    });
  }

  [Test]
  public void TryParse_WithVerboseFlag_SetsVerbose()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--verbose" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsVerbose, Is.True);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithVerboseAlias_SetsVerbose()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "-v" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsVerbose, Is.True);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithVerboseEqualsFalse_AcceptsValue()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--verbose=false" }, out var options, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.True);
      Assert.That(options.IsVerbose, Is.False);
      Assert.That(error, Is.Null);
    });
  }

  [Test]
  public void TryParse_WithDuplicateVerboseFlag_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--verbose", "--verbose" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --verbose option can only be specified once."));
    });
  }

  [Test]
  public void TryParse_WithVerboseAndAlias_ReturnsError()
  {
    bool success = CommandLineOptionsParser.TryParse(new[] { "--verbose", "-v" }, out _, out string? error);

    Assert.Multiple(() =>
    {
      Assert.That(success, Is.False);
      Assert.That(error, Is.EqualTo("The --verbose option can only be specified once."));
    });
  }
}
