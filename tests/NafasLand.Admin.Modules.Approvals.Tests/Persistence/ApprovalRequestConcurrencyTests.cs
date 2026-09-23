using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Persistence;

namespace NafasLand.Admin.Modules.Approvals.Tests.Persistence;

/// <summary>Acceptance criterion 4 / ADR-010 rule 3: two simultaneous decisions on the same row — the second SaveChanges must see a concurrency conflict, not silently overwrite the first.</summary>
public sealed class ApprovalRequestConcurrencyTests
{
    [Fact]
    public async Task دو_تصمیم_هم‌زمان_روی_یک_درخواست_دومی_را_با_تداخل_رد_می‌کند()
    {
        var databaseName = Guid.NewGuid().ToString();
        var requestId = Guid.Empty;

        await using (var seedContext = ApprovalsDbContextTestFactory.Create(databaseName))
        {
            var request = ApprovalRequest.Create(
                "catalog.product.delete", "Product", "101", "{}", null, "دلیل",
                Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
            seedContext.ApprovalRequests.Add(request);
            await seedContext.SaveChangesAsync();
            requestId = request.Id;
        }

        // Two independent scopes load the same row before either writes —
        // simulating two reviewers racing on the same Pending request.
        await using var firstContext = ApprovalsDbContextTestFactory.Create(databaseName);
        await using var secondContext = ApprovalsDbContextTestFactory.Create(databaseName);

        var firstCopy = await firstContext.ApprovalRequests.SingleAsync(request => request.Id == requestId);
        var secondCopy = await secondContext.ApprovalRequests.SingleAsync(request => request.Id == requestId);

        firstCopy.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, "اولی");

        // SQL Server generates a fresh rowversion on every UPDATE by itself; the
        // InMemory provider used by these tests has no such server-side behavior
        // and never mutates a byte[] rowversion column on its own (verified: it
        // stays an empty array across saves). This one line stands in for that
        // server-side assignment so the scenario under test — a stale
        // OriginalValue causing SaveChanges to reject the second writer — is
        // reproducible without a real SQL Server. IsRowVersion() on
        // ApprovalRequestConfiguration is what makes EF compare this value at
        // all; that configuration, not this simulated write, is what's under test.
        firstContext.Entry(firstCopy).Property(request => request.RowVersion).CurrentValue = [1, 2, 3, 4, 5, 6, 7, 8];
        await firstContext.SaveChangesAsync();

        secondCopy.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "دومی");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

        await using var verifyContext = ApprovalsDbContextTestFactory.Create(databaseName);
        var persisted = await verifyContext.ApprovalRequests.SingleAsync(request => request.Id == requestId);
        Assert.Equal(ApprovalRequestStatus.Approved, persisted.Status);
    }
}
