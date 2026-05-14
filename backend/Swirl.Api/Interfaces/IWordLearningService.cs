using Swirl.Api.Requests;
using Swirl.Api.Responses;

namespace Swirl.Api.Interfaces;

public interface IWordLearningService
{
    Task<List<WordResponse>> GetLevelWordsAsync(
        Guid userId,
        int levelId,
        CancellationToken cancellationToken = default);

    Task<MarkLevelWordsLearnedResponse> MarkLevelWordsLearnedAsync(
        Guid userId,
        int levelId,
        MarkLevelWordsLearnedRequest request,
        CancellationToken cancellationToken = default);
}
