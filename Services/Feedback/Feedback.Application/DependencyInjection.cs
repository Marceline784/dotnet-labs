using Feedback.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Feedback.Application;

public static class DependencyInjection
{
    /// <summary>Реєструє MediatR (з автоматичним скануванням обробників), Pipeline Behaviors і валідатори.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = config["LuckyPenny:LicenseKey"];        // Community-ліцензія; без ключа лише попередження в лозі
            cfg.RegisterServicesFromAssembly(assembly);

            // ПОРЯДОК має значення: першим зареєстрований стає найзовнішнім
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));         // 1: зовнішній, бачить усе, зокрема відхилені запити
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));      // 2: відсікає некоректні дані ДО обробника
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));     // 3: найближчий до обробника, міряє його час
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
