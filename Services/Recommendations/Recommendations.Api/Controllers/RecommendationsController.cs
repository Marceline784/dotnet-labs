using Microsoft.AspNetCore.Mvc;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Services;

namespace Recommendations.Api.Controllers;

/// <summary>Рекомендації образів на день. Погоду клієнт НЕ передає: сервіс отримує її сам за профілем.</summary>
[ApiController]
[Route("api/recommendations")]
[Produces("application/json")]
public class RecommendationsController(IRecommendationService recommendations) : ControllerBase
{
    /// <summary>
    /// Створює рекомендацію: автоматична погода -> відбір речей -> образи для каруселі.
    /// Усе зберігається в одній транзакції (або все, або нічого).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<RecommendationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RecommendationDto>> Create(CreateRecommendationDto input, CancellationToken ct)
    {
        var created = await recommendations.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<RecommendationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationDto>> GetById(long id, CancellationToken ct)
        => Ok(await recommendations.GetAsync(id, ct));

    /// <summary>Історія рекомендацій користувача, найновіші першими.</summary>
    [HttpGet("~/api/users/{userId:long}/recommendations")]
    [ProducesResponseType<IReadOnlyList<RecommendationSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RecommendationSummaryDto>>> GetByUser(
        long userId, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await recommendations.GetByUserAsync(userId, page, pageSize, ct));
}
