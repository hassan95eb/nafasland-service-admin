namespace NafasLand.Admin.Shared.Kernel.Approvals;

/// <summary>
/// The outcome of <see cref="IApprovalExecutor.ExecuteAsync"/> (ADR-010, rule 4):
/// a failed upstream call is reported here, as <c>ExecutionFailed</c> with the
/// stored error, never as an unhandled exception that would also undo the
/// reviewer's decision.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public string? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);
}
