namespace Collectibles.Domain.Common.Enums;

public enum JobStatus
{
    NotStart,
    Queueing,
    Doing,
    Done,
    Pending,
    Failed,
    Cancelled,

    /// <summary>
    /// The job ran to completion but some entries failed. Distinct from
    /// <see cref="Done"/> so a partially failed import is not reported as fully successful.
    /// </summary>
    DoneWithErrors,
}
