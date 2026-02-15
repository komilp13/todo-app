using TodoApp.Domain.Enums;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;

namespace TodoApp.Domain.Entities;

/// <summary>
/// Represents a task/todo item.
/// </summary>
public class TodoTask
{
    /// <summary>
    /// Unique identifier (ULID).
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User who owns this task.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Task name (required, max 500 chars).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Task description (optional, max 4000 chars).
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Task due date (optional).
    /// </summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Task priority level (P1-P4).
    /// </summary>
    public Priority Priority { get; private set; }

    /// <summary>
    /// Task completion status.
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// GTD system list assignment.
    /// </summary>
    public SystemList SystemList { get; private set; }

    /// <summary>
    /// Manual sort order within the list.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Optional project this task belongs to.
    /// </summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>
    /// Whether task is archived/completed.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Timestamp when task was completed (if done).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Timestamp when task was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when task was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    /// <summary>
    /// User who owns this task.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Project this task belongs to (optional).
    /// </summary>
    public Project? Project { get; private set; }

    /// <summary>
    /// Labels assigned to this task.
    /// </summary>
    public ICollection<Label> Labels { get; private set; } = new List<Label>();

    /// <summary>
    /// Factory method to create a new task.
    /// </summary>
    public static TodoTask Create(
        Guid userId,
        string name,
        string? description = null,
        SystemList systemList = SystemList.Inbox,
        Priority priority = Priority.P4,
        Guid? projectId = null,
        DateTime? dueDate = null)
    {
        return new TodoTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Priority = priority,
            Status = TaskStatus.Open,
            SystemList = systemList,
            SortOrder = 0,
            ProjectId = projectId,
            DueDate = dueDate,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update task name.
    /// </summary>
    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task priority.
    /// </summary>
    public void UpdatePriority(Priority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task system list.
    /// </summary>
    public void UpdateSystemList(SystemList systemList)
    {
        SystemList = systemList;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task due date.
    /// </summary>
    public void UpdateDueDate(DateTime? dueDate)
    {
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update task project ID.
    /// </summary>
    public void UpdateProjectId(Guid? projectId)
    {
        ProjectId = projectId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark task as complete: sets Done status, archives it, and records completion time.
    /// Idempotent - can be called multiple times safely.
    /// </summary>
    public void Complete()
    {
        Status = TaskStatus.Done;
        IsArchived = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reopen a completed task: sets Open status, unarchives it, clears completion time.
    /// Retains original system list and places task at top (sortOrder = 0).
    /// Idempotent - can be called multiple times safely.
    /// </summary>
    public void Reopen()
    {
        Status = TaskStatus.Open;
        IsArchived = false;
        CompletedAt = null;
        SortOrder = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    private TodoTask() { }
}
