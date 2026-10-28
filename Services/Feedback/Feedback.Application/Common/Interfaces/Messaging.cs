using MediatR;

namespace Feedback.Application.Common.Interfaces;

/// <summary>Команда без результату (CQRS: операція ЗАПИСУ/зміни/видалення).</summary>
public interface ICommand : IRequest { }

/// <summary>Команда з результатом.</summary>
public interface ICommand<TResponse> : IRequest<TResponse> { }

/// <summary>Запит (CQRS: операція ЧИТАННЯ, стан системи не змінює).</summary>
public interface IQuery<TResponse> : IRequest<TResponse> { }
