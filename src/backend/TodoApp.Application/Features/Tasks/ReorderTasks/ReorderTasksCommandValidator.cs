using FluentValidation;

namespace TodoApp.Application.Features.Tasks.ReorderTasks;

/// <summary>
/// Validator for ReorderTasksCommand using FluentValidation.
/// </summary>
public class ReorderTasksCommandValidator : AbstractValidator<ReorderTasksCommand>
{
    public ReorderTasksCommandValidator()
    {
        // TaskIds validation
        RuleFor(x => x.TaskIds)
            .NotNull().WithMessage("Task IDs array is required.")
            .NotEmpty().WithMessage("Task IDs array must not be empty.");

        RuleFor(x => x.TaskIds)
            .Must(taskIds => taskIds.All(id => id != Guid.Empty))
            .WithMessage("All task IDs must be valid non-empty GUIDs.")
            .When(x => x.TaskIds != null);

        // SystemList validation
        RuleFor(x => x.SystemList)
            .IsInEnum().WithMessage("System list must be a valid value (Inbox, Next, Upcoming, or Someday).");

        // UserId validation (required, must be set by API layer)
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID must be set by the API layer.");
    }
}
