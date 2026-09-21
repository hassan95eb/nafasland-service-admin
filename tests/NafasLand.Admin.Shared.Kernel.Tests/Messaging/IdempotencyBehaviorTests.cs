using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class IdempotencyBehaviorTests
{
    [Fact]
    public async Task ارسال_دوباره_همان_فرم_پاسخ_اول_را_بدون_اجرای_دوم_handler_می‌دهد()
    {
        var store = new MemoryIdempotencyStore();
        var context = CreateHttpContext(Guid.NewGuid());
        var behavior = CreateBehavior(store, context);
        var command = new FakeIdempotentCommand("same-body");
        var handlerCalls = 0;
        Task<FakeResponse> Next()
        {
            handlerCalls++;
            return Task.FromResult(new FakeResponse("portal-result"));
        }

        var first = await behavior.HandleAsync(command, Next, CancellationToken.None);
        var second = await behavior.HandleAsync(command, Next, CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, handlerCalls);
    }

    [Fact]
    public async Task کلید_InProgress_با_409_و_بدون_اجرای_handler_رد_می‌شود()
    {
        var store = new MemoryIdempotencyStore { ForcedOutcome = IdempotencyBeginOutcome.InProgress };
        var behavior = CreateBehavior(store, CreateHttpContext(Guid.NewGuid()));
        var handlerCalls = 0;

        var exception = await Assert.ThrowsAsync<ConflictException>(() => behavior.HandleAsync(
            new FakeIdempotentCommand("body"),
            () =>
            {
                handlerCalls++;
                return Task.FromResult(new FakeResponse("unexpected"));
            },
            CancellationToken.None));

        Assert.Contains("در حال انجام", exception.Message);
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public async Task نبودن_هدر_برای_command_idempotent_خطای_اعتبارسنجی_است()
    {
        var context = CreateHttpContext(Guid.NewGuid());
        context.Request.Headers.Remove("Idempotency-Key");
        var behavior = CreateBehavior(new MemoryIdempotencyStore(), context);

        var exception = await Assert.ThrowsAsync<CommandValidationException>(() => behavior.HandleAsync(
            new FakeIdempotentCommand("body"),
            () => Task.FromResult(new FakeResponse("unexpected")),
            CancellationToken.None));

        Assert.Contains("Idempotency-Key", exception.Errors.Keys);
    }

    [Fact]
    public async Task شکست_handler_رکورد_InProgress_را_حذف_می‌کند()
    {
        var store = new MemoryIdempotencyStore();
        var behavior = CreateBehavior(store, CreateHttpContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(
            new FakeIdempotentCommand("body"),
            () => throw new InvalidOperationException("failed"),
            CancellationToken.None));

        Assert.Equal(1, store.RemoveCalls);
    }

    private static IdempotencyBehavior<FakeIdempotentCommand, FakeResponse> CreateBehavior(
        IIdempotencyStore store,
        DefaultHttpContext context)
    {
        var services = new ServiceCollection()
            .AddSingleton(store)
            .BuildServiceProvider();
        var accessor = new HttpContextAccessor { HttpContext = context };
        return new IdempotencyBehavior<FakeIdempotentCommand, FakeResponse>(services, accessor);
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test"));
        context.Request.Headers["Idempotency-Key"] = Guid.NewGuid().ToString();
        return context;
    }

    private sealed record FakeIdempotentCommand(string Body) : ICommand<FakeResponse>, IIdempotentCommand;

    private sealed record FakeResponse(string Value);

    private sealed class MemoryIdempotencyStore : IIdempotencyStore
    {
        private Stored? _stored;

        public IdempotencyBeginOutcome? ForcedOutcome { get; init; }

        public int RemoveCalls { get; private set; }

        public Task<IdempotencyBeginResult> TryBeginAsync(
            Guid key,
            Guid userId,
            string requestHash,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            if (ForcedOutcome is { } forced)
            {
                return Task.FromResult(new IdempotencyBeginResult(forced));
            }

            if (_stored is null)
            {
                _stored = new Stored(key, userId, requestHash, null);
                return Task.FromResult(new IdempotencyBeginResult(IdempotencyBeginOutcome.Started));
            }

            if (_stored.Key != key || _stored.UserId != userId || _stored.RequestHash != requestHash)
            {
                return Task.FromResult(new IdempotencyBeginResult(IdempotencyBeginOutcome.KeyReused));
            }

            return Task.FromResult(_stored.ResponseJson is null
                ? new IdempotencyBeginResult(IdempotencyBeginOutcome.InProgress)
                : new IdempotencyBeginResult(IdempotencyBeginOutcome.Completed, _stored.ResponseJson));
        }

        public Task CompleteAsync(Guid key, string responseJson, CancellationToken cancellationToken)
        {
            _stored = _stored! with { ResponseJson = responseJson };
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid key, CancellationToken cancellationToken)
        {
            RemoveCalls++;
            _stored = null;
            return Task.CompletedTask;
        }

        private sealed record Stored(Guid Key, Guid UserId, string RequestHash, string? ResponseJson);
    }
}
