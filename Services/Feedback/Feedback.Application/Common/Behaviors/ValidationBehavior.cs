using FluentValidation;
using MediatR;

namespace Feedback.Application.Common.Behaviors;

/// <summary>
/// Автоматично виконує ВСІ валідатори FluentValidation для кожної команди й запиту.
/// Помилки збираються разом; якщо вони є, кидається ValidationException, а ОБРОБНИК НЕ ВИКЛИКАЄТЬСЯ.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);   // обробник не викличеться

        return await next();
    }
}
