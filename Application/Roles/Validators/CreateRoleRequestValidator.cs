using Application.Roles.Requests;
using FluentValidation;

namespace Application.Roles.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name must not be whitespace.")
            .MaximumLength(100);
    }
}
