using FluentValidation;

namespace TodoApp.Application.Features.Tasks.GetTasks;

/// <summary>
/// Validator for GetTasksQuery using FluentValidation.
/// </summary>
public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        // SystemList validation (optional, but if provided must be valid enum)
        RuleFor(x => x.SystemList)
            .NotNull()
            .When(x => x.SystemList.HasValue)
            .WithMessage("SystemList must be a valid value (Inbox, Next, Upcoming, or Someday).");

        // ProjectId validation (optional, but if provided must be valid)
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .When(x => x.ProjectId.HasValue)
            .WithMessage("ProjectId must be a valid GUID.");

        // LabelId validation (optional, but if provided must be valid)
        RuleFor(x => x.LabelId)
            .NotEmpty()
            .When(x => x.LabelId.HasValue)
            .WithMessage("LabelId must be a valid GUID.");

        // Status validation (must be Open, Done, or All)
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s == "Open" || s == "Done" || s == "All")
            .WithMessage("Status must be 'Open', 'Done', or 'All'.");

        // UserId validation (required, set by API layer)
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId must be set by the API layer.");
    }
}
