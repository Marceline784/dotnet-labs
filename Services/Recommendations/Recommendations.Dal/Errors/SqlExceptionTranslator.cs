using Microsoft.Data.SqlClient;
using Recommendations.Domain.Constants;
using Recommendations.Domain.Exceptions;

namespace Recommendations.Dal.Errors;

/// <summary>
/// Перетворює SqlException на доменні винятки, щоб сирі SQL-повідомлення не потрапляли в API.
/// Номери 5xxxx кидають наші процедури з Лаб. 01 (THROW), 2601/2627 — порушення UNIQUE, 547 — порушення FK.
/// </summary>
public static class SqlExceptionTranslator
{
    private const int ForeignKeyViolation = 547;

    /// <summary>Повертає доменний виняток або null, якщо помилка невідома (тоді її кидають далі як є).</summary>
    public static DomainException? Translate(SqlException ex) => ex.Number switch
    {
        SqlErrorCodes.UserNotFound
            or SqlErrorCodes.RecommendationNotFound
            or SqlErrorCodes.OutfitNotFound
            or SqlErrorCodes.SlotNotFound
            or SqlErrorCodes.ItemOwnerNotFound => new NotFoundException(ex.Message),

        SqlErrorCodes.EmptyOutfit => new ValidationException(ex.Message),

        SqlErrorCodes.ItemsNotAvailable
            or SqlErrorCodes.DuplicateSlotLayer
            or SqlErrorCodes.OutfitWornRecently
            or SqlErrorCodes.OutfitAlreadyInCarousel
            or SqlErrorCodes.InvalidStatusTransition
            or SqlErrorCodes.AnotherOutfitChosen => new BusinessConflictException(ex.Message),

        SqlErrorCodes.UniqueIndexViolation
            or SqlErrorCodes.UniqueConstraintViolation => new BusinessConflictException("Record with the same key already exists"),

        ForeignKeyViolation => new BusinessConflictException("Related record does not exist or is still in use"),

        _ => null
    };
}
