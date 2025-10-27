namespace Onboard.Core.Tests.Services;

using Onboard.Core.Services;

[TestFixture]
public class CommandLineOptionsParserTests
{
    [Test]
    public void TryParseMode_WhenArgsIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CommandLineOptionsParser.TryParseMode(null!, out _, out _));
    }

    [Test]
    public void TryParseMode_WhenModeNotSpecified_ReturnsFalseMode()
    {
        bool success = CommandLineOptionsParser.TryParseMode(Array.Empty<string>(), out bool isWslGuestMode, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(isWslGuestMode, Is.False);
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void TryParseMode_WithSeparateArguments_SetsWslGuestMode()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode", "wsl-guest" }, out bool isWslGuestMode, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(isWslGuestMode, Is.True);
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void TryParseMode_WithEqualsSyntax_SetsWslGuestMode()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode=wsl-guest" }, out bool isWslGuestMode, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(isWslGuestMode, Is.True);
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void TryParseMode_WithMissingValue_ReturnsError()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode" }, out bool _, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("The --mode option requires a value."));
        });
    }

    [Test]
    public void TryParseMode_WithEmptyValue_ReturnsError()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode=" }, out bool _, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("The --mode option requires a non-empty value."));
        });
    }

    [Test]
    public void TryParseMode_WithInvalidValue_ReturnsError()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode", "invalid" }, out bool _, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Unsupported --mode value 'invalid'. Expected 'wsl-guest'."));
        });
    }

    [Test]
    public void TryParseMode_WithDuplicateOption_ReturnsError()
    {
        bool success = CommandLineOptionsParser.TryParseMode(new[] { "--mode=wsl-guest", "--mode=wsl-guest" }, out bool _, out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("The --mode option can only be specified once."));
        });
    }
}
