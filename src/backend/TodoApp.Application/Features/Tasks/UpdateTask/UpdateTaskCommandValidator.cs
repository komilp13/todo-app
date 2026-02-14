using FluentValidation;

namespace TodoApp.Application.Features.Tasks.UpdateTask;

/// <summary>
/// Validator for UpdateTaskCommand using FluentValidation.
/// Only validates fields that are provided (non-null).
/// </summary>
public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        // Task ID is always required
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("TaskId must be a valid GUID.");

        // User ID is always required (set by API layer)
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId must be set by the API layer.");

        // Name validation (optional, only if provided)
        RuleFor(x => x.Name)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must not exceed 500 characters.");

        // Description validation (optional, only if provided)
        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 4000 characters.");

        // Priority validation (optional, only if provided)
        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue)
            .WithMessage("Priority must be a valid value (P1, P2, P3, or P4).");

        // SystemList validation (optional, only if provided)
        RuleFor(x => x.SystemList)
            .IsInEnum()
            .When(x => x.SystemList.HasValue)
            .WithMessage("SystemList must be a valid value (Inbox, Next, Upcoming, or Someday).");

        // ProjectId validation (optional, but if provided must be valid GUID)
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .When(x => x.HasProjectId && x.ProjectId.HasValue)
            .WithMessage("ProjectId must be a valid GUID.");
    }
}
