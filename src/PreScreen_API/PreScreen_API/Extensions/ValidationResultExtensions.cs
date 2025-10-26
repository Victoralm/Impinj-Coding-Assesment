using PreScreen_API.Models;

namespace PreScreen_API.Extensions;

public static class ValidationResultExtensions
{
    public static ResultDto<T> ToResultDtoFailure<T>(this FluentValidation.Results.ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
            .ToArray();

        return ResultDto<T>.Failure(errors);
    }
}
