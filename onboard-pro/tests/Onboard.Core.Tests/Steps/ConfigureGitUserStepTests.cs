// <copyright file="ConfigureGitUserStepTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Tests.Steps;

using Moq;
using NUnit.Framework;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Shared;

[TestFixture]
public class ConfigureGitUserStepTests
{
  [Test]
  public async Task ShouldExecuteAsync_WhenNameNotConfigured_ReturnsTrue()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    // Simulate git config user.name not set (exit code 1)
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, string.Empty));

    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.email"))
      .ReturnsAsync(new ProcessResult(0, "test@example.com", string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    bool result = await step.ShouldExecuteAsync();

    // Assert
    Assert.That(result, Is.True);
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenEmailNotConfigured_ReturnsTrue()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name"))
      .ReturnsAsync(new ProcessResult(0, "Test User", string.Empty));

    // Simulate git config user.email not set (exit code 1)
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.email"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    bool result = await step.ShouldExecuteAsync();

    // Assert
    Assert.That(result, Is.True);
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenBothConfigured_ReturnsFalse()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name"))
      .ReturnsAsync(new ProcessResult(0, "Test User", string.Empty));

    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.email"))
      .ReturnsAsync(new ProcessResult(0, "test@example.com", string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    bool result = await step.ShouldExecuteAsync();

    // Assert
    Assert.That(result, Is.False);
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenNameIsEmpty_ReturnsTrue()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    // Empty output even though exit code is 0
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name"))
      .ReturnsAsync(new ProcessResult(0, "  ", string.Empty));

    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.email"))
      .ReturnsAsync(new ProcessResult(0, "test@example.com", string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    bool result = await step.ShouldExecuteAsync();

    // Assert
    Assert.That(result, Is.True);
  }

  [Test]
  public async Task ExecuteAsync_WhenConfigMissing_PromptsUserAndRunsCommands()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    // Simulate user input
    mockUI.Setup(ui => ui.Ask("Please enter your full name for Git commits:", null)).Returns("Test User");
    mockUI.Setup(ui => ui.Ask("Please enter your email for Git commits:", null)).Returns("test@example.com");

    // Simulate successful git config commands
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name \"Test User\""))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.email \"test@example.com\""))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    await step.ExecuteAsync();

    // Assert
    mockProcessRunner.Verify(p => p.RunAsync("git", "config --global user.name \"Test User\""), Times.Once);
    mockProcessRunner.Verify(p => p.RunAsync("git", "config --global user.email \"test@example.com\""), Times.Once);
    mockUI.Verify(ui => ui.WriteSuccess("Git user configured as 'Test User <test@example.com>'."), Times.Once);
  }

  [Test]
  public async Task ExecuteAsync_WhenNameIsEmpty_RepromptsUntilValid()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    int promptCount = 0;
    mockUI
      .Setup(ui => ui.Ask("Please enter your full name for Git commits:", null))
      .Returns(() => promptCount++ == 0 ? string.Empty : "Test User");

    mockUI.Setup(ui => ui.Ask("Please enter your email for Git commits:", null)).Returns("test@example.com");

    mockProcessRunner
      .Setup(p => p.RunAsync("git", It.IsAny<string>()))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act
    await step.ExecuteAsync();

    // Assert
    mockUI.Verify(ui => ui.Ask("Please enter your full name for Git commits:", null), Times.Exactly(2));
    mockUI.Verify(ui => ui.WriteWarning("Name cannot be empty."), Times.Once);
  }

  [Test]
  public void ExecuteAsync_WhenGitConfigFails_ThrowsException()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();

    mockUI.Setup(ui => ui.Ask("Please enter your full name for Git commits:", null)).Returns("Test User");
    mockUI.Setup(ui => ui.Ask("Please enter your email for Git commits:", null)).Returns("test@example.com");

    // Simulate git config failure
    mockProcessRunner
      .Setup(p => p.RunAsync("git", "config --global user.name \"Test User\""))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "fatal: unable to write config"));

    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act & Assert
    var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await step.ExecuteAsync().ConfigureAwait(false));
    Assert.That(ex.Message, Does.Contain("Failed to set git user.name"));
  }

  [Test]
  public void Description_ReturnsExpectedValue()
  {
    // Arrange
    var mockProcessRunner = new Mock<IProcessRunner>();
    var mockUI = new Mock<IUserInteraction>();
    var step = new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

    // Act & Assert
    Assert.That(step.Description, Is.EqualTo("Configure Git user identity"));
  }
}
