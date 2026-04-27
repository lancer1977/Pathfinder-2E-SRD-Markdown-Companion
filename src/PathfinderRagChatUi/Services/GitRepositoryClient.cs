using System.Diagnostics;

namespace PathfinderRagChatUi.Services;

public interface IGitRepositoryClient
{
    Task<string> CloneFreshAsync(string repositoryUrl, string branch, string destinationRoot, CancellationToken cancellationToken);

    Task<string?> GetHeadCommitAsync(string repositoryPath, CancellationToken cancellationToken);
}

public sealed class GitRepositoryClient : IGitRepositoryClient
{
    public async Task<string> CloneFreshAsync(string repositoryUrl, string branch, string destinationRoot, CancellationToken cancellationToken)
    {
        var destination = Path.GetFullPath(destinationRoot, AppContext.BaseDirectory);
        var parent = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        var args = $"clone --depth 1 --branch {Quote(branch)} {Quote(repositoryUrl)} {Quote(destination)}";
        await RunGitAsync(args, Path.GetDirectoryName(destination) ?? AppContext.BaseDirectory, cancellationToken);
        return destination;
    }

    public async Task<string?> GetHeadCommitAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var result = await RunGitAsync("rev-parse HEAD", repositoryPath, cancellationToken, throwOnFailure: false);
        return string.IsNullOrWhiteSpace(result.StdOut) ? null : result.StdOut.Trim();
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunGitAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken,
        bool throwOnFailure = true)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (throwOnFailure && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments} failed: {stderr.Trim()}");
        }

        return (process.ExitCode, stdout, stderr);
    }
}
