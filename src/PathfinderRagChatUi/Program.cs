using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using PathfinderRagChatUi.Components;
using PathfinderRagChatUi.Contracts;
using PathfinderRagChatUi.Endpoints;
using PathfinderRagChatUi.Data;
using PathfinderRagChatUi.Options;
using PathfinderRagChatUi.Services;
using Seq.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSeq(builder.Configuration.GetSection("Seq"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHealthChecks();

builder.Services.AddOptions<CorpusOptions>()
    .BindConfiguration("Corpus")
    .Validate(options => !string.IsNullOrWhiteSpace(options.RepositoryUrl), "Corpus:RepositoryUrl is required")
    .ValidateOnStart();

builder.Services.AddOptions<OllamaOptions>()
    .BindConfiguration("Ollama")
    .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), "Ollama:BaseUrl is required")
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration("Database")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Path), "Database:Path is required")
    .ValidateOnStart();

builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
    var dbPath = Path.GetFullPath(dbOptions.Path, AppContext.BaseDirectory);
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var ollama = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(ollama.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<IGitRepositoryClient, GitRepositoryClient>();
builder.Services.AddSingleton<MarkdownChunker>();
builder.Services.AddScoped<CorpusRefreshService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<AppQueryService>();

var refreshOnly = args.Any(a => string.Equals(a, "--refresh-database", StringComparison.OrdinalIgnoreCase));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

if (refreshOnly)
{
    using var scope = app.Services.CreateScope();
    var refreshService = scope.ServiceProvider.GetRequiredService<CorpusRefreshService>();
    await refreshService.RefreshAsync(null, CancellationToken.None);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/api/health");
app.MapAppApi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
