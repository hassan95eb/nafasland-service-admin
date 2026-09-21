using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// Runs after authorization and before the command transaction. The idempotency
/// row is intentionally committed independently from the command transaction.
/// </summary>
internal sealed class IdempotencyBehavior<TCommand, TResponse>(
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (command is not IIdempotentCommand)
        {
            return await next();
        }

        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Idempotency requires an HTTP request context.");
        var key = ReadKey(httpContext.Request.Headers);
        var userId = ReadUserId(httpContext.User);
        var requestHash = ComputeRequestHash(command);
        var store = serviceProvider.GetRequiredService<IIdempotencyStore>();

        var begin = await store.TryBeginAsync(
            key,
            userId,
            requestHash,
            DateTimeOffset.UtcNow,
            cancellationToken);

        switch (begin.Outcome)
        {
            case IdempotencyBeginOutcome.Completed:
                return DeserializeResponse(begin.ResponseJson);
            case IdempotencyBeginOutcome.InProgress:
                throw new ConflictException("این درخواست در حال انجام است.");
            case IdempotencyBeginOutcome.KeyReused:
                throw ValidationError("Idempotency-Key", "این کلید قبلاً برای درخواست دیگری استفاده شده است.");
            case IdempotencyBeginOutcome.Started:
                break;
            default:
                throw new InvalidOperationException("Unknown idempotency state.");
        }

        try
        {
            var response = await next();
            var responseJson = JsonSerializer.Serialize(response, SerializerOptions);
            await store.CompleteAsync(key, responseJson, cancellationToken);
            return response;
        }
        catch
        {
            await store.RemoveAsync(key, cancellationToken);
            throw;
        }
    }

    private static Guid ReadKey(IHeaderDictionary headers)
    {
        var raw = headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw ValidationError("Idempotency-Key", "هدر Idempotency-Key الزامی است.");
        }

        if (!Guid.TryParse(raw, out var key))
        {
            throw ValidationError("Idempotency-Key", "هدر Idempotency-Key باید یک GUID معتبر باشد.");
        }

        return key;
    }

    private static Guid ReadUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }

        return userId;
    }

    private static string ComputeRequestHash(TCommand command)
    {
        var json = JsonSerializer.Serialize(command, command.GetType(), SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static TResponse DeserializeResponse(string? responseJson)
    {
        if (responseJson is null)
        {
            throw new InvalidOperationException("Completed idempotency record has no response.");
        }

        return JsonSerializer.Deserialize<TResponse>(responseJson, SerializerOptions)
            ?? throw new InvalidOperationException("Stored idempotency response could not be deserialized.");
    }

    private static CommandValidationException ValidationError(string field, string message)
    {
        return new CommandValidationException(new Dictionary<string, string[]>
        {
            [field] = [message],
        });
    }
}
