using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Api.Infrastructure;
using Wardrobe.Bll.Common;
using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Services;

namespace Wardrobe.Api.Controllers;

/// <summary>Речі гардероба. Тонкий контролер: без бізнес-правил і без доступу до DbContext.</summary>
[ApiController]
[Route("api/wardrobe-items")]
[Produces("application/json")]
public class WardrobeItemsController(IWardrobeItemService service) : ControllerBase
{
    /// <summary>
    /// Список із пагінацією, фільтрацією та сортуванням (усе виконується в SQL).
    /// Метадані пагінації повертаються і в тілі, і в заголовках X-Total-Count, X-Page, X-Page-Size, X-Total-Pages.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<WardrobeItemListDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<WardrobeItemListDto>>> GetList(
        [FromQuery] WardrobeItemQuery query,
        [FromServices] IValidator<WardrobeItemQuery> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        var result = await service.GetListAsync(query, ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result);
    }

    /// <summary>Картка речі з деталями, фото й категоріями (один eager-запит).</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> Create(
        CreateWardrobeItemDto input,
        [FromServices] IValidator<CreateWardrobeItemDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> Update(
        long id,
        UpdateWardrobeItemDto input,
        [FromServices] IValidator<UpdateWardrobeItemDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        return Ok(await service.UpdateAsync(id, input, ct));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---------- прання ----------

    /// <summary>Відправити річ у прання. Повторно: 409.</summary>
    [HttpPost("{id:long}/laundry")]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> MarkInLaundry(long id, CancellationToken ct)
        => Ok(await service.MarkInLaundryAsync(id, ct));

    [HttpDelete("{id:long}/laundry")]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> ReturnFromLaundry(long id, CancellationToken ct)
        => Ok(await service.ReturnFromLaundryAsync(id, ct));

    /// <summary>Речі користувача, які зараз у пранні (специфікація ItemsInLaundrySpec).</summary>
    [HttpGet("~/api/users/{userId:long}/laundry")]
    [ProducesResponseType<IReadOnlyList<WardrobeItemListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WardrobeItemListDto>>> GetInLaundry(long userId, CancellationToken ct)
        => Ok(await service.GetInLaundryAsync(userId, ct));

    // ---------- деталі (1:1) ----------

    [HttpPut("{id:long}/detail")]
    [ProducesResponseType<ItemDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDetailDto>> SetDetail(
        long id,
        ItemDetailInputDto input,
        [FromServices] IValidator<ItemDetailInputDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        return Ok(await service.SetDetailAsync(id, input, ct));
    }

    // ---------- фото (1:N) ----------

    [HttpPost("{id:long}/images")]
    [ProducesResponseType<ItemImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemImageDto>> AddImage(
        long id,
        AddImageDto input,
        [FromServices] IValidator<AddImageDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        var image = await service.AddImageAsync(id, input, ct);
        return CreatedAtAction(nameof(GetById), new { id }, image);
    }

    [HttpPut("{id:long}/images/{imageId:long}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(long id, long imageId, CancellationToken ct)
    {
        await service.SetPrimaryImageAsync(id, imageId, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}/images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(long id, long imageId, CancellationToken ct)
    {
        await service.RemoveImageAsync(id, imageId, ct);
        return NoContent();
    }

    // ---------- категорії (M:N) ----------

    [HttpPost("{id:long}/categories")]
    [ProducesResponseType<WardrobeItemDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WardrobeItemDetailsDto>> AddToCategory(long id, AddToCategoryDto input, CancellationToken ct)
        => Ok(await service.AddToCategoryAsync(id, input, ct));

    [HttpDelete("{id:long}/categories/{categoryId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCategory(long id, long categoryId, CancellationToken ct)
    {
        await service.RemoveFromCategoryAsync(id, categoryId, ct);
        return NoContent();
    }

    /// <summary>Речі категорії, додані за останні N днів (LINQ через проміжну сутність M:N, поле AddedAt).</summary>
    [HttpGet("recently-added")]
    [ProducesResponseType<IReadOnlyList<WardrobeItemListDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WardrobeItemListDto>>> GetRecentlyAdded(
        [FromQuery] string category, [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await service.GetRecentlyAddedAsync(category, days, ct));
}
