using FluentValidation;
using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.CreateTask;

/// <summary>
/// Validator for CreateTaskCommand using FluentValidation.
/// </summary>
public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(500).WithMessage("Task name must not exceed 500 characters.");

        // Description validation
        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Task description must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Priority validation
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be a valid value (P1, P2, P3, or P4).");

        // SystemList validation
        RuleFor(x => x.SystemList)
            .IsInEnum().WithMessage("System list must be a valid value (Inbox, Next, Upcoming, or Someday).");

        // DueDate validation (optional, but if provided, should be valid)
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Due date must be in the future or today.")
            .When(x => x.DueDate.HasValue);

        // ProjectId validation (optional, but if provided, should be a valid GUID)
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID must be a valid GUID.")
            .When(x => x.ProjectId.HasValue);

        // UserId validation (required, must be set by API layer)
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID must be set by the API layer.");
    }
}
