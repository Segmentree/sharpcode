using System.Diagnostics;
using System.Text.Json;
using SharpCode.Api.Data;
using SharpCode.Runner.Contracts;

namespace SharpCode.Api.Judge;

/// <summary>
/// Compiles a submission and executes its test cases in a separate, disposable runner process so that
/// infinite loops, stack overflows or process exits in user code cannot take the API down.
/// </summary>
public sealed class JudgeService(Compiler compiler, ILogger<JudgeService> logger)
{
    private static readonly string RunnerDll = Path.Combine(AppContext.BaseDirectory, "SharpCode.Runner.dll");
    private static readonly string DotnetHost = GetDotnetHost();

    public async Task<JudgeResult> JudgeAsync(Problem problem, IReadOnlyList<TestCase> cases, string code,
        bool revealHidden, CancellationToken ct)
    {
        var overall = Stopwatch.StartNew();
        var compiled = compiler.Compile(code, $"Submission_{Guid.NewGuid():N}");
        if (!compiled.Success)
        {
            var errors = compiled.Diagnostics.Count(d => d.IsError);
            return new JudgeResult(Verdict.CompileError, $"Compilation failed with {errors} error(s).",
                compiled.Diagnostics, [], 0, cases.Count, overall.Elapsed.TotalMilliseconds);
        }

        var workDir = Path.Combine(Path.GetTempPath(), "sharpcode", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var dllPath = Path.Combine(workDir, "Solution.dll");
            var testsPath = Path.Combine(workDir, "tests.json");
            await File.WriteAllBytesAsync(dllPath, compiled.Assembly!, ct);
            await File.WriteAllTextAsync(testsPath,
                JsonSerializer.Serialize(cases.Select(c => new RunnerTestCase(c.Id, c.Input, c.Expected))), ct);

            var perTest = Math.Clamp(problem.TimeLimitMs, 100, 30_000);
            var budget = TimeSpan.FromMilliseconds(perTest * cases.Count + 3_000);

            var (runnerResults, fatal, timedOut, stderr) =
                await RunAsync(dllPath, problem.ClassName, problem.MethodName, testsPath, perTest, budget, ct);

            if (fatal is not null)
                return new JudgeResult(Verdict.RuntimeError, fatal, compiled.Diagnostics, [], 0, cases.Count,
                    overall.Elapsed.TotalMilliseconds);

            var byId = runnerResults.ToDictionary(r => r.Id);
            var results = new List<TestCaseResult>(cases.Count);
            var sawTimeout = false;
            foreach (var c in cases)
            {
                var hideDetails = c.IsHidden && !revealHidden;
                if (byId.TryGetValue(c.Id, out var r))
                {
                    var tle = r.DurationMs >= perTest;
                    sawTimeout |= tle;
                    results.Add(new TestCaseResult(c.Id, r.Passed && !tle, c.IsHidden,
                        hideDetails ? null : c.Input, hideDetails ? null : c.Expected,
                        hideDetails ? null : r.Actual, hideDetails ? null : r.Stdout,
                        tle ? $"Time limit exceeded ({perTest} ms)." : r.Error, r.DurationMs));
                }
                else
                {
                    // No result came back: the runner was killed (timeout) or crashed (stack overflow, Environment.Exit...)
                    var error = timedOut
                        ? "Not run: an earlier test case exceeded the time limit."
                        : "The runner process crashed before this test finished." +
                          (string.IsNullOrWhiteSpace(stderr) ? "" : $"\n{Truncate(stderr)}");
                    sawTimeout |= timedOut;
                    results.Add(new TestCaseResult(c.Id, false, c.IsHidden,
                        hideDetails ? null : c.Input, hideDetails ? null : c.Expected, null, null, error, 0));
                    if (!timedOut) break; // crash: later tests never ran either
                }
            }
            // Pad any remaining cases after a crash so counts line up.
            foreach (var c in cases.Skip(results.Count))
                results.Add(new TestCaseResult(c.Id, false, c.IsHidden, c.IsHidden && !revealHidden ? null : c.Input,
                    c.IsHidden && !revealHidden ? null : c.Expected, null, null, "Not run.", 0));

            var passed = results.Count(r => r.Passed);
            var verdict = passed == results.Count ? Verdict.Accepted
                : sawTimeout ? Verdict.TimeLimitExceeded
                : results.Any(r => r.Error is not null) ? Verdict.RuntimeError
                : Verdict.WrongAnswer;
            var message = verdict switch
            {
                Verdict.Accepted => $"All {results.Count} test case(s) passed.",
                Verdict.TimeLimitExceeded => "Your solution exceeded the time limit.",
                Verdict.RuntimeError => "Your solution threw an exception.",
                _ => $"{passed}/{results.Count} test case(s) passed."
            };
            return new JudgeResult(verdict, message, compiled.Diagnostics, results, passed, results.Count,
                overall.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Judge failed");
            return new JudgeResult(Verdict.JudgeError, "The judge hit an internal error: " + ex.Message,
                compiled.Diagnostics, [], 0, cases.Count, overall.Elapsed.TotalMilliseconds);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static async Task<(List<RunnerTestResult> Results, string? Fatal, bool TimedOut, string Stderr)> RunAsync(
        string dllPath, string className, string methodName, string testsPath, int timeLimitMs, TimeSpan budget,
        CancellationToken ct)
    {
        if (!File.Exists(RunnerDll))
            throw new FileNotFoundException($"Runner not found at {RunnerDll}. Build the solution first.");

        var psi = new ProcessStartInfo(DotnetHost)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath)!,
        };
        psi.ArgumentList.Add(RunnerDll);
        psi.ArgumentList.Add(dllPath);
        psi.ArgumentList.Add(className);
        psi.ArgumentList.Add(methodName);
        psi.ArgumentList.Add(testsPath);
        psi.ArgumentList.Add(timeLimitMs.ToString());
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start runner.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(budget);

        var results = new List<RunnerTestResult>();
        string? fatal = null;
        var timedOut = false;
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            while (await process.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
            {
                if (line.StartsWith(RunnerProtocol.ResultPrefix))
                {
                    var r = JsonSerializer.Deserialize<RunnerTestResult>(line[RunnerProtocol.ResultPrefix.Length..]);
                    if (r is not null) results.Add(r);
                }
                else if (line.StartsWith(RunnerProtocol.FatalPrefix))
                {
                    fatal = line[RunnerProtocol.FatalPrefix.Length..];
                }
            }
            await process.WaitForExitAsync(timeout.Token);
            timedOut = process.ExitCode == RunnerProtocol.TimeoutExitCode;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            timedOut = true;
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            }
        }

        var stderr = await stderrTask;
        return (results, fatal, timedOut, stderr);
    }

    private static string GetDotnetHost()
    {
        // Prefer the host that is running us so the runner uses the exact same runtime.
        var current = Process.GetCurrentProcess().MainModule?.FileName;
        if (current is not null && Path.GetFileNameWithoutExtension(current).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            return current;
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (dotnetRoot is not null)
        {
            var candidate = Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(candidate)) return candidate;
        }
        return "dotnet";
    }

    /// <summary>Keeps the first lines of a crash report (e.g. "Stack overflow." + a few frames) readable.</summary>
    private static string Truncate(string s)
    {
        var lines = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var head = string.Join('\n', lines.Take(8));
        return lines.Length > 8 ? head + "\n…(truncated)" : head;
    }
}
