using Recommendations.Domain.Models;

namespace Recommendations.Dal.Interfaces;

/// <summary>
/// Репозиторій користувачів на ЧИСТОМУ ADO.NET. Свідомо НЕ успадковує IGenericRepository
/// (за вимогою методички): демонструє роботу з підключенням, параметрами та транзакцією без сторонніх бібліотек.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(User user, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);

    Task<UserProfile?> GetProfileAsync(long userId, CancellationToken ct = default);
    Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default);
}
