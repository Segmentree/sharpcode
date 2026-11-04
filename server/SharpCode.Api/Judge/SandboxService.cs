using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SharpCode.Api.Judge;

public enum SandboxStatus { Ok, CompileError, RuntimeError, TimeLimitExceeded }

public sealed record SandboxResult(SandboxStatus Status, IReadOnlyList<CompileDiagnostic> Diagnostics, string Stdout,
    string Stderr, int? ExitCode, bool TimedOut, double DurationMs);

/// <summary>Compiles a free-form console program and runs it with the given stdin, like an online compiler.</summary>
public sealed class SandboxService(Compiler compiler, ILogger<SandboxService> logger)
{
    private static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(5);
    private const int MaxOutputChars = 64 * 1024;

    public async Task<SandboxResult> RunAsync(string code, string stdin, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var compiled = compiler.Compile(code, $"Sandbox_{Guid.NewGuid():N}", console: true);
        if (!compiled.Success)
            return new SandboxResult(SandboxStatus.CompileError, compiled.Diagnostics, "", "", null, false, sw.Elapsed.TotalMilliseconds);

        var workDir = Path.Combine(Path.GetTempPath(), "sharpcode", "sandbox", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var dll = Path.Combine(workDir, "Program.dll");
            await File.WriteAllBytesAsync(dll, compiled.Assembly!, ct);
            await File.WriteAllTextAsync(Path.Combine(workDir, "Program.runtimeconfig.json"), RuntimeConfig(), ct);

            var psi = new ProcessStartInfo(JudgeService.DotnetHost)
            {
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workDir,
            };
            psi.ArgumentList.Add(dll);
            psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            psi.Environment["DOTNET_NOLOGO"] = "1";

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start program.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeLimit);

            // Feed stdin up front; a program that reads past the end simply gets null from ReadLine().
            try
            {
                await process.StandardInput.WriteAsync(stdin);
                process.StandardInput.Close();
            }
            catch (IOException) { /* program exited before reading */ }

            var stdoutTask = ReadCappedAsync(process.StandardOutput, timeout.Token);
            var stderrTask = ReadCappedAsync(process.StandardError, timeout.Token);
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                timedOut = true;
                try { process.Kill(entireProcessTree: true); } catch { /* gone */ }
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = timedOut ? (int?)null : process.ExitCode;
            var status = timedOut ? SandboxStatus.TimeLimitExceeded
                : exitCode == 0 ? SandboxStatus.Ok
                : SandboxStatus.RuntimeError;
            return new SandboxResult(status, compiled.Diagnostics, stdout, stderr, exitCode, timedOut, sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Sandbox run failed");
            return new SandboxResult(SandboxStatus.RuntimeError, compiled.Diagnostics, "", "Sandbox error: " + ex.Message, null, false,
                sw.Elapsed.TotalMilliseconds);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static async Task<string> ReadCappedAsync(StreamReader reader, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        try
        {
            int n;
            while ((n = await reader.ReadAsync(buffer, ct)) > 0)
            {
                if (sb.Length < MaxOutputChars) sb.Append(buffer, 0, Math.Min(n, MaxOutputChars - sb.Length));
                else truncated = true;
            }
        }
        catch (OperationCanceledException) { /* killed on timeout; keep what we have */ }
        if (truncated || sb.Length >= MaxOutputChars) sb.Append("\n…(output truncated)");
        return sb.ToString();
    }

    private static string RuntimeConfig()
    {
        var v = Environment.Version;
        return JsonSerializer.Serialize(new
        {
            runtimeOptions = new
            {
                tfm = $"net{v.Major}.{v.Minor}",
                framework = new { name = "Microsoft.NETCore.App", version = $"{v.Major}.{v.Minor}.0" },
                configProperties = new Dictionary<string, object> { ["System.Globalization.Invariant"] = true },
            },
        });
    }
}
