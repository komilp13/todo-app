using FluentValidation;

namespace TodoApp.Application.Features.Labels.CreateLabel;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required.")
            .MaximumLength(100).WithMessage("Label name must be 100 characters or less.");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9a-fA-F]{6}$")
            .When(x => x.Color != null)
            .WithMessage("Color must be a valid hex color (e.g., '#ff4040').");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
