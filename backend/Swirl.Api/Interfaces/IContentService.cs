using Swirl.Api.Responses;

namespace Swirl.Api.Interfaces;

public interface IContentService
{
    Task<List<SectionResponse>> GetSectionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SectionResponse?> GetSectionAsync(
        Guid userId,
        int sectionId,
        CancellationToken cancellationToken = default);

    Task<List<LevelResponse>?> GetSectionLevelsAsync(
        Guid userId,
        int sectionId,
        CancellationToken cancellationToken = default);

    Task<LevelDetailsResponse?> GetLevelAsync(
        Guid userId,
        int levelId,
        CancellationToken cancellationToken = default);
}
