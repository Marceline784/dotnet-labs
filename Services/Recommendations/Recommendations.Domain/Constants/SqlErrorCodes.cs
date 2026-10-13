namespace Recommendations.Domain.Constants;
public static class SqlErrorCodes
{
    // ---- NotFound (404) ----
    public const int UserNotFound = 50001;
    public const int RecommendationNotFound = 50003;
    public const int OutfitNotFound = 50021;
    public const int SlotNotFound = 50031;
    public const int ItemOwnerNotFound = 50041;

    // ---- Validation (400) ----
    public const int EmptyOutfit = 50011;

    // ---- BusinessConflict (409) ----
    public const int ItemsNotAvailable = 50012;
    public const int DuplicateSlotLayer = 50013;
    public const int OutfitWornRecently = 50014;
    public const int OutfitAlreadyInCarousel = 50015;
    public const int InvalidStatusTransition = 50022;
    public const int AnotherOutfitChosen = 50023;

    // ---- системні: порушення UNIQUE ----
    public const int UniqueIndexViolation = 2601;
    public const int UniqueConstraintViolation = 2627;
}
