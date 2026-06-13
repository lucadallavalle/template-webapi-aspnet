using Microsoft.Extensions.Logging;

namespace WebApiTemplate.Application.Logging;

/// <summary>
/// Base class for logging decorators.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public abstract class BaseLoggingDecorator<TRequest>
{
    private readonly ILogger<BaseLoggingDecorator<TRequest>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseLoggingDecorator{TRequest}"/> class.
    /// </summary>
    /// <param name="logger"></param>
    protected BaseLoggingDecorator(ILogger<BaseLoggingDecorator<TRequest>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles and logs the message.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="next">The next delegate to call.</param>
    /// <typeparam name="TResult">The result type of the request.</typeparam>
    /// <returns>The result of the decorated handler.</returns>
    protected async Task<TResult> HandleAndLogMessage<TResult>(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResult>> next
    )
    {
        // Log the request *type name* only — never the request instance. Serializing the request
        // (e.g. "{@request}") can leak PII / secrets carried in command and query payloads.
        var requestType = typeof(TRequest).Name;

        _logger.LogInformation("START handling request {RequestType}", requestType);
        try
        {
            var result = await next(request, cancellationToken);
            _logger.LogInformation("FINISH handling request {RequestType}", requestType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED handling request {RequestType}", requestType);
            throw;
        }
    }
}
