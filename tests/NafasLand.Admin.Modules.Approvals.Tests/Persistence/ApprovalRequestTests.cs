using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Persistence;

/// <summary>Acceptance criterion 4: a decision on a terminal request is rejected, never overwritten.</summary>
public sealed class ApprovalRequestTests
{
    private static ApprovalRequest CreatePending() => ApprovalRequest.Create(
        "catalog.product.delete", "Product", "101", "{}", null, "دلیل تست",
        Guid.NewGuid(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void تازه‌ساخته_در_وضعیت_Pending_است()
    {
        var request = CreatePending();
        Assert.Equal(ApprovalRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void تأیید_از_Pending_به_Approved_می‌رود_و_اطلاعات_بازبین_را_ثبت_می‌کند()
    {
        var request = CreatePending();
        var reviewerId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        request.MarkApproved(reviewerId, now, "یادداشت");

        Assert.Equal(ApprovalRequestStatus.Approved, request.Status);
        Assert.Equal(reviewerId, request.ReviewedByUserId);
        Assert.Equal(now, request.ReviewedAt);
        Assert.Equal("یادداشت", request.ReviewNote);
    }

    // InlineData cannot carry an internal enum on a public Theory method (the
    // parameter would be less accessible than the method itself); the status
    // name is passed as a string and parsed back inside the test instead.
    [Theory]
    [InlineData(nameof(ApprovalRequestStatus.Approved))]
    [InlineData(nameof(ApprovalRequestStatus.Rejected))]
    [InlineData(nameof(ApprovalRequestStatus.Executed))]
    [InlineData(nameof(ApprovalRequestStatus.ExecutionFailed))]
    [InlineData(nameof(ApprovalRequestStatus.Cancelled))]
    [InlineData(nameof(ApprovalRequestStatus.Expired))]
    public void تصمیم_روی_درخواست_ترمینال_رد_می‌شود(string terminalStatusName)
    {
        var terminalStatus = Enum.Parse<ApprovalRequestStatus>(terminalStatusName);
        var request = CreatePending();
        MoveToTerminal(request, terminalStatus);

        Assert.Throws<ConflictException>(() => request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null));
        Assert.Throws<ConflictException>(() => request.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "یادداشت"));
        Assert.Throws<ConflictException>(() => request.MarkCancelled(DateTime.UtcNow));

        // The status must not have changed as a side effect of the rejected attempts.
        Assert.Equal(terminalStatus, request.Status);
    }

    [Fact]
    public void رد_نیاز_به_Pending_دارد()
    {
        var request = CreatePending();
        request.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "یادداشت رد");

        Assert.Equal(ApprovalRequestStatus.Rejected, request.Status);
        Assert.Throws<ConflictException>(() => request.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "دوباره"));
    }

    [Fact]
    public void لغو_نیاز_به_Pending_دارد()
    {
        var request = CreatePending();
        request.MarkCancelled(DateTime.UtcNow);

        Assert.Equal(ApprovalRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void اجرای_موفق_وضعیت_را_Executed_می‌کند_و_خطای_قبلی_را_پاک_می‌کند()
    {
        var request = CreatePending();
        request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
        request.MarkExecutionFailed(DateTime.UtcNow, "خطای اول");

        request.MarkExecuted(DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(ApprovalRequestStatus.Executed, request.Status);
        Assert.Null(request.ExecutionError);
    }

    [Fact]
    public void تلاش_دوباره_فقط_از_ExecutionFailed_مجاز_است()
    {
        var request = CreatePending();
        Assert.Throws<ConflictException>(() => request.EnsureExecutionFailed());

        request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
        request.MarkExecutionFailed(DateTime.UtcNow, "خطا");
        request.EnsureExecutionFailed();
    }

    [Fact]
    public void انقضا_فقط_از_Pending_مجاز_است()
    {
        var request = CreatePending();
        request.MarkCancelled(DateTime.UtcNow);

        Assert.Throws<ConflictException>(request.MarkExpired);
    }

    private static void MoveToTerminal(ApprovalRequest request, ApprovalRequestStatus status)
    {
        switch (status)
        {
            case ApprovalRequestStatus.Approved:
                request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
                break;
            case ApprovalRequestStatus.Rejected:
                request.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "یادداشت");
                break;
            case ApprovalRequestStatus.Executed:
                request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
                request.MarkExecuted(DateTime.UtcNow);
                break;
            case ApprovalRequestStatus.ExecutionFailed:
                request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
                request.MarkExecutionFailed(DateTime.UtcNow, "خطا");
                break;
            case ApprovalRequestStatus.Cancelled:
                request.MarkCancelled(DateTime.UtcNow);
                break;
            case ApprovalRequestStatus.Expired:
                request.MarkExpired();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
