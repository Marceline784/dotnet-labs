using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Api.Infrastructure;
using Wardrobe.Bll.Dtos;
using Wardrobe.Bll.Services;

namespace Wardrobe.Api.Controllers;

/// <summary>Довідник категорій (тип речі та сезон).</summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    /// <summary>Категорія з речами (explicit loading: два окремі SQL-запити).</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<CategoryWithItemsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryWithItemsDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetWithItemsAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(
        CategoryInputDto input,
        [FromServices] IValidator<CategoryInputDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Rename(
        long id,
        CategoryInputDto input,
        [FromServices] IValidator<CategoryInputDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToErrors()));

        return Ok(await service.RenameAsync(id, input, ct));
    }

    /// <summary>Категорію з речами видалити не можна (409).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
