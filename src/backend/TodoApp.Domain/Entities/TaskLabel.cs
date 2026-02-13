namespace TodoApp.Domain.Entities;

/// <summary>
/// Join entity representing the many-to-many relationship between tasks and labels.
/// </summary>
public class TaskLabel
{
    /// <summary>
    /// Task ID (part of composite primary key).
    /// </summary>
    public Guid TaskId { get; private set; }

    /// <summary>
    /// Label ID (part of composite primary key).
    /// </summary>
    public Guid LabelId { get; private set; }

    // Navigation properties
    /// <summary>
    /// The task.
    /// </summary>
    public TodoTask Task { get; private set; } = null!;

    /// <summary>
    /// The label.
    /// </summary>
    public Label Label { get; private set; } = null!;

    /// <summary>
    /// Factory method to create a task-label association.
    /// </summary>
    public static TaskLabel Create(Guid taskId, Guid labelId)
    {
        return new TaskLabel
        {
            TaskId = taskId,
            LabelId = labelId
        };
    }

    private TaskLabel() { }
}
