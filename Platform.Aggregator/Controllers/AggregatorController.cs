using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public class AggregatorController(RecommendationCardService service, ILogger<AggregatorController> logger) : ControllerBase
{
    /// <summary>Повна картка рекомендації: рекомендація + речі з гардероба + відгуки на образи (+ попередження).</summary>
    [HttpGet("recommendations/{id:long}/full")]
    [ProducesResponseType<RecommendationCardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RecommendationCardDto>> GetFull(long id, CancellationToken ct)
    {
        try
        {
            var card = await service.GetAsync(id, ct);
            return card is null
                ? Problem(statusCode: StatusCodes.Status404NotFound, title: $"Recommendation {id} not found")
                : Ok(card);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "recommendations-api is unavailable for recommendation {RecommendationId}", id);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "recommendations-api is unavailable");
        }
    }
}
