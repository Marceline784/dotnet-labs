using AutoMapper;
using Microsoft.Extensions.Logging;
using Recommendations.Bll.Dtos;
using Recommendations.Dal.Interfaces;
using Recommendations.Domain.Exceptions;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Services;

public sealed class UserService(IUnitOfWork uow, IMapper mapper, ILogger<UserService> logger) : IUserService
{
    private static readonly string[] Styles = { "Casual", "Sport", "Classic", "Street" };

    public async Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken ct = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var displayName = input.DisplayName.Trim();
        if (displayName.Length == 0)
            throw new ValidationException("Display name must not be empty");

        // бізнес-правило, яке потребує стану БД
        if (await uow.Users.GetByEmailAsync(email, ct) is not null)
            throw new BusinessConflictException($"User with email '{email}' already exists");

        var user = new User { Email = email, DisplayName = displayName };
        user.Id = await uow.Users.AddAsync(user, ct);

        logger.LogInformation("User {UserId} created", user.Id);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> GetAsync(long id, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"User {id} not found");
        return mapper.Map<UserDto>(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default) =>
        mapper.Map<IReadOnlyList<UserDto>>(await uow.Users.GetAllAsync(ct));

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await uow.Users.DeleteAsync(id, ct))
            throw new NotFoundException($"User {id} not found");
        logger.LogInformation("User {UserId} deleted (soft)", id);
    }

    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var profile = await uow.Users.GetProfileAsync(userId, ct)
            ?? throw new NotFoundException($"Profile of user {userId} not found");
        return mapper.Map<UserProfileDto>(profile);
    }

    public async Task<UserProfileDto> SaveProfileAsync(long userId, SaveUserProfileDto input, CancellationToken ct = default)
    {
        if (await uow.Users.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException($"User {userId} not found");

        var city = input.City.Trim();
        if (city.Length == 0)
            throw new ValidationException("City must not be empty");
        if (input.StylePreference is not null && !Styles.Contains(input.StylePreference))
            throw new ValidationException($"Style must be one of: {string.Join(", ", Styles)}");

        var profile = new UserProfile
        {
            UserId = userId,
            City = city,
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            ColdSensitivity = input.ColdSensitivity,
            StylePreference = input.StylePreference
        };
        await uow.Users.SaveProfileAsync(profile, ct);
        return mapper.Map<UserProfileDto>(profile);
    }
}
