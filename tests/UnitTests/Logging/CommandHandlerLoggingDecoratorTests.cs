using AwesomeAssertions;
using HumbleMediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebApiTemplate.Application.Logging;
using Xunit;

namespace WebApiTemplate.UnitTests.Logging;

public class CommandHandlerLoggingDecoratorTests
{
    private const string Secret = "super-secret-PII";

    // A command carrying sensitive data that must never end up in the logs. Public so NSubstitute
    // (Castle DynamicProxy) can build a proxy of ICommandHandler<SecretCommand, int>.
    public sealed record SecretCommand(string Token) : ICommand<int>;

    [Fact]
    public async Task LogsRequestTypeNameButNeverTheRequestBody()
    {
        // Arrange
        var decorated = Substitute.For<ICommandHandler<SecretCommand, int>>();
        decorated.Handle(Arg.Any<SecretCommand>(), Arg.Any<CancellationToken>()).Returns(7);

        var logger = new RecordingLogger<CommandHandlerLoggingDecorator<SecretCommand, int>>();
        var sut = new CommandHandlerLoggingDecorator<SecretCommand, int>(decorated, logger);

        // Act
        var result = await sut.Handle(new SecretCommand(Secret), CancellationToken.None);

        // Assert
        result.Should().Be(7);
        logger.Entries.Should().Contain(e => e.Message.Contains(nameof(SecretCommand)));
        logger.Entries.Should().NotContain(e => e.Message.Contains(Secret));
    }

    [Fact]
    public async Task RethrowsAndLogsErrorWithoutLeakingTheRequestBody()
    {
        // Arrange
        var boom = new InvalidOperationException("boom");
        var decorated = Substitute.For<ICommandHandler<SecretCommand, int>>();
        decorated
            .Handle(Arg.Any<SecretCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(boom));

        var logger = new RecordingLogger<CommandHandlerLoggingDecorator<SecretCommand, int>>();
        var sut = new CommandHandlerLoggingDecorator<SecretCommand, int>(decorated, logger);

        // Act
        Func<Task> act = () => sut.Handle(new SecretCommand(Secret), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception == boom);
        logger.Entries.Should().NotContain(e => e.Message.Contains(Secret));
    }
}
