using FluentValidation;

namespace TodoApp.Application.Features.Labels.UpdateLabel;

public class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
{
    public UpdateLabelCommandValidator()
    {
        RuleFor(x => x.LabelId)
            .NotEmpty().WithMessage("Label ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Label name must be 100 characters or less.")
            .When(x => x.Name != null);

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9a-fA-F]{6}$")
            .When(x => x.HasColor && x.Color != null)
            .WithMessage("Color must be a valid hex color (e.g., '#ff4040').");
    }
}
