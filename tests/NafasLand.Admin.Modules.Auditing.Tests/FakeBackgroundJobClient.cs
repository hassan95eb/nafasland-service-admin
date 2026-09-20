using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace NafasLand.Admin.Modules.Auditing.Tests;

/// <summary>Records what would have been enqueued, without needing a real Hangfire storage.</summary>
internal sealed class FakeBackgroundJobClient : IBackgroundJobClient
{
    public List<Job> CreatedJobs { get; } = [];

    public string Create(Job job, IState state)
    {
        CreatedJobs.Add(job);
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedState) => true;
}
