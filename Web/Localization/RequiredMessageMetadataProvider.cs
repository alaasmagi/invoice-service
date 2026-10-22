using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Web.Localization;

/// <summary>
/// Gives validation attributes without an explicit message (including MVC's implicit [Required] on
/// non-nullable values) a message key, so data-annotations localization can translate it.
/// </summary>
public class RequiredMessageMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType != null) continue;
            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "The {0} field is required.",
                StringLengthAttribute { MinimumLength: > 0 } => "The {0} field must be between {2} and {1} characters long.",
                StringLengthAttribute => "The {0} field can be at most {1} characters long.",
                RangeAttribute => "The {0} field must be between {1} and {2}.",
                EmailAddressAttribute => "The {0} field is not a valid email address.",
                _ => attribute.ErrorMessage
            };
        }
    }
}
