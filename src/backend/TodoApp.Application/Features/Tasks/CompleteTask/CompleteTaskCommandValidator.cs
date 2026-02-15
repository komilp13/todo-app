using FluentValidation;

namespace TodoApp.Application.Features.Tasks.CompleteTask;

/// <summary>
/// Validator for CompleteTaskCommand.
/// </summary>
public class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
