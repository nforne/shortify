using FluentValidation;
using Shortify.DTOs.AuthOptionDTOs;

namespace Shortify.Models.AuthOptions
{
    public class SignUpDtoValidator : AbstractValidator<SignUpDto>
    {
        public SignUpDtoValidator()
        {
            RuleFor(x => x.TenantName).NotEmpty();

            RuleFor(x => x.AdminEmail)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.AdminPassword)
                .NotEmpty()
                .MinimumLength(8);

            // Optional name fields length constraints
            RuleFor(x => x.DisplayName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.LastName));

            // Ensure at least one of DisplayName, FirstName or LastName is provided
            RuleFor(x => x).Custom((dto, context) =>
            {
                var hasDisplay = !string.IsNullOrWhiteSpace(dto.DisplayName);
                var hasFirst = !string.IsNullOrWhiteSpace(dto.FirstName);
                var hasLast = !string.IsNullOrWhiteSpace(dto.LastName);

                if (!hasDisplay && !hasFirst && !hasLast)
                {
                    context.AddFailure("At least one of DisplayName, FirstName or LastName must be provided.");
                }
            });
        }
    }
}
