using Microsoft.AspNetCore.Mvc;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Services;

namespace Recommendations.Api.Controllers;

/// <summary>
/// Речі гардероба (локальна копія для читання). Власник даних — сервіс «Гардероб»;
/// PUT імітує застосування події від нього (Лаб. 08).
/// </summary>
[ApiController]
[Route("api/wardrobe-items")]
[Produces("application/json")]
public class WardrobeController(IWardrobeService wardrobe) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<WardrobeItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WardrobeItemDto>> GetById(long id, CancellationToken ct)
        => Ok(await wardrobe.GetAsync(id, ct));

    [HttpPut("{id:long}")]
    [ProducesResponseType<WardrobeItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WardrobeItemDto>> Upsert(long id, UpsertWardrobeItemDto input, CancellationToken ct)
    {
        if (input.Id != id)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation error",
                detail: "Id in the route and in the body must match");

        return Ok(await wardrobe.UpsertAsync(input, ct));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await wardrobe.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Гардероб користувача; за потреби лише певний слот (Top, Bottom, Shoes, Bag, Accessory).</summary>
    [HttpGet("~/api/users/{userId:long}/wardrobe")]
    [ProducesResponseType<IReadOnlyList<WardrobeItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WardrobeItemDto>>> GetByUser(
        long userId, [FromQuery] string? slot, CancellationToken ct)
        => Ok(await wardrobe.GetByUserAsync(userId, slot, ct));

    /// <summary>Речі, які підходять під погоду цієї рекомендації (пул для каруселі).</summary>
    [HttpGet("~/api/recommendations/{recommendationId:long}/eligible-items")]
    [ProducesResponseType<IReadOnlyList<EligibleItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EligibleItemDto>>> GetEligible(long recommendationId, CancellationToken ct)
        => Ok(await wardrobe.GetEligibleAsync(recommendationId, ct));
}
