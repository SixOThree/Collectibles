namespace Collectibles.Domain.Common.Enums;

/// <summary>
/// Helpers for reasoning about <see cref="JobStatus"/> without enumerating the terminal
/// values at every call site.
/// </summary>
public static class JobStatusExtensions
{
    /// <summary>
    /// Whether the job finished processing, with or without per-entry errors.
    /// </summary>
    /// <returns></returns>
    public static bool IsCompleted(this JobStatus status)
        => status is JobStatus.Done or JobStatus.DoneWithErrors;

    /// <summary>
    /// Whether the job has reached a state it will not leave on its own.
    /// </summary>
    /// <returns></returns>
    public static bool IsTerminal(this JobStatus status)
        => status.IsCompleted() || status is JobStatus.Failed or JobStatus.Cancelled;
}
