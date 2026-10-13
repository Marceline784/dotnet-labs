using Recommendations.Bll.Dtos;

namespace Recommendations.Bll.Services;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken ct = default);
    Task<UserDto> GetAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);

    Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<UserProfileDto> SaveProfileAsync(long userId, SaveUserProfileDto input, CancellationToken ct = default);
}
