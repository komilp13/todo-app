using FluentValidation;

namespace TodoApp.Application.Features.Projects.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name != null);
        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.HasDescription && x.Description != null);
    }
}
