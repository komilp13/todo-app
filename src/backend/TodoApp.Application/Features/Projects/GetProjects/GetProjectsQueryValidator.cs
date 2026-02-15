using FluentValidation;

namespace TodoApp.Application.Features.Projects.GetProjects;

/// <summary>
/// Validator for GetProjectsQuery.
/// </summary>
public class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
