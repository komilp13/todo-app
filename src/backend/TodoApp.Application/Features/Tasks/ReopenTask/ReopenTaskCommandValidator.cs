using FluentValidation;

namespace TodoApp.Application.Features.Tasks.ReopenTask;

/// <summary>
/// Validator for ReopenTaskCommand.
/// </summary>
public class ReopenTaskCommandValidator : AbstractValidator<ReopenTaskCommand>
{
    public ReopenTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
