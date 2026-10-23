using AutoMapper;
using Microsoft.Extensions.Logging;
using Wardrobe.Bll.Dtos;
using Wardrobe.Dal.Interfaces;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Exceptions;

namespace Wardrobe.Bll.Services;

public sealed class CategoryService(IUnitOfWork uow, IMapper mapper, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => mapper.Map<IReadOnlyList<CategoryDto>>((await uow.Categories.GetAllAsync(ct)).OrderBy(c => c.Name).ToList());

    public async Task<CategoryWithItemsDto> GetWithItemsAsync(long id, CancellationToken ct = default)
    {
        // EXPLICIT LOADING: категорія окремим запитом, речі окремим (2 SQL-запити)
        var category = await uow.Categories.GetWithItemsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        return mapper.Map<CategoryWithItemsDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CategoryInputDto input, CancellationToken ct = default)
    {
        var name = input.Name.Trim();
        if (await uow.Categories.ExistsByNameAsync(name, null, ct))
            throw new ConflictException($"Category '{name}' already exists");

        var category = new Category(name);
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} created", category.Id);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> RenameAsync(long id, CategoryInputDto input, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        var name = input.Name.Trim();
        if (await uow.Categories.ExistsByNameAsync(name, id, ct))
            throw new ConflictException($"Category '{name}' already exists");

        category.Rename(name);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetWithItemsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        // правило, якому потрібні дані з БД: категорію з речами видаляти не можна
        if (category.ItemCategories.Count > 0)
            throw new ConflictException("The category still contains items and cannot be deleted");

        uow.Categories.Delete(category);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Category {CategoryId} deleted", id);
    }
}
