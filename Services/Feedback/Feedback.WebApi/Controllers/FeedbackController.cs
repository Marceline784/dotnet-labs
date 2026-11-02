using AutoMapper;
using Feedback.Application.Common.Models;
using Feedback.Application.Feedbacks.Commands.AddComment;
using Feedback.Application.Feedbacks.Commands.CreateFeedback;
using Feedback.Application.Feedbacks.Commands.DeleteFeedback;
using Feedback.Application.Feedbacks.Commands.UpdateFeedback;
using Feedback.Application.Feedbacks.Dtos;
using Feedback.Application.Feedbacks.Queries.GetFeedbackById;
using Feedback.Application.Feedbacks.Queries.GetFeedbackList;
using Feedback.WebApi.Contracts;
using Feedback.WebApi.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace Feedback.WebApi.Controllers;

/// <summary>
/// Відгуки на образи. Кожна операція виконується командою або запитом MediatR;
/// контролер не містить бізнес-логіки й не знає про MongoDB.
/// </summary>
[Route("api/feedback")]
[Produces("application/json")]
public class FeedbackController(IMapper mapper) : ApiControllerBase
{
    /// <summary>Список із фільтрами, повнотекстовим пошуком (search) і пагінацією на рівні MongoDB.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<FeedbackDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<FeedbackDto>>> GetList(
        [FromQuery] long? outfitId, [FromQuery] long? authorUserId, [FromQuery] string? feeling,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(
            new GetFeedbackListQuery(outfitId, authorUserId, feeling, search, page, pageSize), ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result);
    }

    /// <summary>Повний відгук. Версія документа повертається в заголовку ETag.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<FeedbackDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackDetailsDto>> GetById(string id, CancellationToken ct)
    {
        var feedback = await Mediator.Send(new GetFeedbackByIdQuery(id), ct);
        SetETag(feedback.Version);
        return Ok(feedback);
    }

    /// <summary>Створити відгук: 201 + Location, у тілі рядковий ObjectId.</summary>
    [HttpPost]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdResponse>> Create(CreateFeedbackRequest request, CancellationToken ct)
    {
        var id = await Mediator.Send(mapper.Map<CreateFeedbackCommand>(request), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
    }

    /// <summary>
    /// Змінити відгук. Заголовок If-Match передає версію, яку бачив клієнт (з ETag):
    /// якщо документ уже змінили, повертається 409.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<VersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VersionResponse>> Update(string id, UpdateFeedbackRequest request, CancellationToken ct)
    {
        var expected = ReadIfMatchVersion();
        var command = mapper.Map<UpdateFeedbackCommand>(request, o =>
        {
            o.Items[FeedbackProfile.IdKey] = id;
            if (expected is not null) o.Items[FeedbackProfile.VersionKey] = expected.Value;
        });

        var version = await Mediator.Send(command, ct);
        SetETag(version);
        return Ok(new VersionResponse(version));
    }

    /// <summary>Додати вкладений коментар (не більше 5; довше обговорення живе окремо).</summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType<VersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VersionResponse>> AddComment(string id, AddCommentRequest request, CancellationToken ct)
    {
        var expected = ReadIfMatchVersion();
        var command = mapper.Map<AddCommentCommand>(request, o =>
        {
            o.Items[FeedbackProfile.IdKey] = id;
            if (expected is not null) o.Items[FeedbackProfile.VersionKey] = expected.Value;
        });

        var version = await Mediator.Send(command, ct);
        SetETag(version);
        return Ok(new VersionResponse(version));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteFeedbackCommand(id), ct);
        return NoContent();
    }
}
