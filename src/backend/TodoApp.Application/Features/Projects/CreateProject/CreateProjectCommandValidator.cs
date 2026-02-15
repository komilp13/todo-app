using FluentValidation;

namespace TodoApp.Application.Features.Projects.CreateProject;

/// <summary>
/// Validator for CreateProjectCommand ensuring all business rules are met.
/// </summary>
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(200)
            .WithMessage("Project name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description != null)
            .WithMessage("Project description cannot exceed 4000 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
