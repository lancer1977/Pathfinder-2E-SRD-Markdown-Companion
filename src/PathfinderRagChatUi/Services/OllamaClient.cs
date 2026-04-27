using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PathfinderRagChatUi.Services;

public interface IOllamaClient
{
    Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<IReadOnlyList<float>>> EmbedAsync(IReadOnlyList<string> inputs, string model, CancellationToken cancellationToken);

    Task<string> GenerateAsync(string system, string prompt, string model, CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamGenerateAsync(string system, string prompt, string model, CancellationToken cancellationToken);
}

public sealed class OllamaClient(HttpClient httpClient) : IOllamaClient
{
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/tags", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("models", out var modelsElement))
        {
            return [];
        }

        var models = new List<string>();
        foreach (var model in modelsElement.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    models.Add(name);
                }
            }
        }

        return models;
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> EmbedAsync(IReadOnlyList<string> inputs, string model, CancellationToken cancellationToken)
    {
        var payload = new { model, input = inputs };
        using var response = await httpClient.PostAsJsonAsync("api/embed", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");

        return envelope.Embeddings
            .Select(x => (IReadOnlyList<float>)x.ToArray())
            .ToArray();
    }

    public async Task<string> GenerateAsync(string system, string prompt, string model, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        await foreach (var token in StreamGenerateAsync(system, prompt, model, cancellationToken))
        {
            builder.Append(token);
        }

        return builder.ToString().Trim();
    }

    public async IAsyncEnumerable<string> StreamGenerateAsync(
        string system,
        string prompt,
        string model,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new
        {
            model,
            system,
            prompt,
            stream = true,
            options = new { temperature = 0.2 }
        };

        using var response = await httpClient.PostAsJsonAsync("api/generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            GenerateStreamResponse? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GenerateStreamResponse>(line);
            }
            catch
            {
                continue;
            }

            if (chunk is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(chunk.Response))
            {
                yield return chunk.Response;
            }

            if (chunk.Done)
            {
                yield break;
            }
        }
    }

    private sealed record EmbedResponse([property: System.Text.Json.Serialization.JsonPropertyName("embeddings")] IReadOnlyList<IReadOnlyList<float>> Embeddings);

    private sealed record GenerateStreamResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("response")] string Response,
        [property: System.Text.Json.Serialization.JsonPropertyName("done")] bool Done);
}
