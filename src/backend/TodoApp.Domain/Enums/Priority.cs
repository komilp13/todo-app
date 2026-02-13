namespace TodoApp.Domain.Enums;

/// <summary>
/// Task priority level (P1 = highest, P4 = lowest).
/// </summary>
public enum Priority
{
    /// <summary>
    /// Highest priority (urgent).
    /// </summary>
    P1 = 1,

    /// <summary>
    /// High priority.
    /// </summary>
    P2 = 2,

    /// <summary>
    /// Medium priority.
    /// </summary>
    P3 = 3,

    /// <summary>
    /// Low priority (default).
    /// </summary>
    P4 = 4
}
