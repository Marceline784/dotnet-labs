using FluentValidation.Results;

namespace Wardrobe.Api.Infrastructure;

public static class ValidationExtensions
{
    /// <summary>Помилки FluentValidation у форматі «поле -> перелік повідомлень» для ValidationProblemDetails.</summary>
    public static Dictionary<string, string[]> ToErrors(this ValidationResult result)
        => result.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
