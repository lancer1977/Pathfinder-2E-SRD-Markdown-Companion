using Microsoft.Extensions.Options;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Options;
using PathfinderRagChatUi.Services;

namespace PathfinderRagChatUi.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapAppApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/models", async (OllamaClient client, CancellationToken cancellationToken) =>
            Results.Ok(new
            {
                models = await client.GetModelsAsync(cancellationToken)
            }));

        group.MapGet("/summary", async (AppQueryService queries, IOptions<CorpusOptions> options, CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetSummaryAsync(options.Value.Name, cancellationToken)));

        group.MapPost("/corpus/refresh", async (CorpusRefreshRequestDto? request, CorpusRefreshService refreshService, CancellationToken cancellationToken) =>
            Results.Ok(await refreshService.RefreshAsync(request, cancellationToken)));

        group.MapPost("/chat", async (ChatRequestDto request, ChatService chatService, CancellationToken cancellationToken) =>
            Results.Ok(await chatService.ChatAsync(request, cancellationToken)));

        group.MapGet("/history", async (AppQueryService queries, IOptions<CorpusOptions> options, int limit, CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetHistoryAsync(options.Value.Name, limit <= 0 ? 50 : limit, cancellationToken)));

        group.MapGet("/pins", async (AppQueryService queries, IOptions<CorpusOptions> options, int limit, CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetPinsAsync(options.Value.Name, limit <= 0 ? 50 : limit, cancellationToken)));

        group.MapPost("/pins", async (PinRequestDto request, AppQueryService queries, IOptions<CorpusOptions> options, CancellationToken cancellationToken) =>
            Results.Ok(await queries.PinAsync(options.Value.Name, request, cancellationToken)));

        group.MapGet("/source/{corpusName}/{chunkId:guid}", async (string corpusName, Guid chunkId, AppQueryService queries, CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetChunkAsync(corpusName, chunkId, cancellationToken)));

        return endpoints;
    }
}
