using Microsoft.AspNetCore.Mvc;
using Recommendations.Bll.Dtos;
using Recommendations.Bll.Services;

namespace Recommendations.Api.Controllers;

/// <summary>Образи з каруселі: перегляд і зміна статусу (обрати, одягнути, скасувати).</summary>
[ApiController]
[Route("api/outfits")]
[Produces("application/json")]
public class OutfitsController(IOutfitService outfits) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<OutfitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OutfitDto>> GetById(long id, CancellationToken ct)
        => Ok(await outfits.GetAsync(id, ct));

    /// <summary>
    /// Змінює статус образу. Недопустимий перехід (наприклад, Generated -> Worn) повертає 409,
    /// неіснуючий образ 404, невідомий статус 400.
    /// </summary>
    [HttpPost("{id:long}/status")]
    [ProducesResponseType<OutfitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OutfitDto>> ChangeStatus(long id, ChangeOutfitStatusDto input, CancellationToken ct)
        => Ok(await outfits.ChangeStatusAsync(id, input, ct));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await outfits.DeleteAsync(id, ct);
        return NoContent();
    }
}
