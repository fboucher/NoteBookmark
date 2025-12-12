using System.ComponentModel.DataAnnotations;

namespace NoteBookmark.Domain;

public class ContainsPlaceholderAttribute : ValidationAttribute
{
    private readonly string _placeholder;

    public ContainsPlaceholderAttribute(string placeholder)
    {
        _placeholder = placeholder;
        ErrorMessage = $"The field must contain '{{{placeholder}}}'.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        string stringValue = value.ToString()!;
        
        if (!stringValue.Contains($"{{{_placeholder}}}"))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
