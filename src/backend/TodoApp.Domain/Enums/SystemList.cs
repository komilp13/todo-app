namespace TodoApp.Domain.Enums;

/// <summary>
/// GTD system lists for organizing tasks.
/// </summary>
public enum SystemList
{
    /// <summary>
    /// Inbox: default entry point for new tasks.
    /// </summary>
    Inbox = 0,

    /// <summary>
    /// Next: tasks ready to work on soon.
    /// </summary>
    Next = 1,

    /// <summary>
    /// Upcoming: tasks with near future due dates.
    /// </summary>
    Upcoming = 2,

    /// <summary>
    /// Someday: tasks deferred for later.
    /// </summary>
    Someday = 3
}
