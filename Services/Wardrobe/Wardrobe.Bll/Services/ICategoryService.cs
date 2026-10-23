using Wardrobe.Bll.Dtos;

namespace Wardrobe.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryWithItemsDto> GetWithItemsAsync(long id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CategoryInputDto input, CancellationToken ct = default);
    Task<CategoryDto> RenameAsync(long id, CategoryInputDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
