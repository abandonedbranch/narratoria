using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace NarratoriaClient.PlaywrightTests;

public sealed class NarratoriaServerFixture : IAsyncLifetime, IDisposable
{
    private Process? _process;
    private readonly int _port;

    public NarratoriaServerFixture()
    {
        _port = GetFreeTcpPort();
    }

    public string BaseUrl => $"http://127.0.0.1:{_port}";

    public async Task InitializeAsync()
    {
        var repoRoot = ResolveRepoRoot();
        var projectPath = Path.Combine(repoRoot, "NarratoriaClient", "NarratoriaClient.csproj");

        var startInfo = new ProcessStartInfo("dotnet", $"run --no-build --project \"{projectPath}\" --urls {BaseUrl}")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.EnvironmentVariables["UseFakeChatService"] = "true";

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to launch Narratoria test server.");
        _process.OutputDataReceived += (_, args) => Debug.WriteLine(args.Data);
        _process.ErrorDataReceived += (_, args) => Debug.WriteLine(args.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitForServerReadyAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        _process?.Dispose();
    }

    private async Task WaitForServerReadyAsync()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (_process?.HasExited == true)
            {
                throw new InvalidOperationException("Narratoria test server exited before becoming ready.");
            }

            try
            {
                var response = await httpClient.GetAsync(BaseUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Ignore until timeout expires.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out while waiting for the Narratoria test server to start.");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "narratoria.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Unable to locate the repository root for Narratoria.");
        }

        return directory.FullName;
    }
}
