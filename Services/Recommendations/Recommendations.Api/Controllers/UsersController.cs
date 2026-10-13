using Microsoft.AspNetCore.Mvc;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Services;

namespace Recommendations.Api.Controllers;

/// <summary>Користувачі та їхні профілі (місто й координати для автоматичної погоди).</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
        => Ok(await users.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(long id, CancellationToken ct)
        => Ok(await users.GetAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto input, CancellationToken ct)
    {
        var created = await users.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await users.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:long}/profile")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfile(long id, CancellationToken ct)
        => Ok(await users.GetProfileAsync(id, ct));

    [HttpPut("{id:long}/profile")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> SaveProfile(long id, SaveUserProfileDto input, CancellationToken ct)
        => Ok(await users.SaveProfileAsync(id, input, ct));
}
