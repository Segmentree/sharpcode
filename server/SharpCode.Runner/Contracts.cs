namespace SharpCode.Runner.Contracts;

/// <summary>A single test case handed to the runner. Input is a JSON array of arguments; Expected is JSON.</summary>
public sealed record RunnerTestCase(int Id, string Input, string Expected);

/// <summary>Result of executing one test case, emitted by the runner as a JSON line on stdout.</summary>
public sealed record RunnerTestResult
{
    public int Id { get; init; }
    public bool Passed { get; init; }
    public string? Actual { get; init; }
    public string? Stdout { get; init; }
    public string? Error { get; init; }
    public double DurationMs { get; init; }
}

public static class RunnerProtocol
{
    /// <summary>Lines starting with this prefix carry a serialized <see cref="RunnerTestResult"/>.</summary>
    public const string ResultPrefix = "@@RESULT ";
    /// <summary>Emitted once when the runner could not even start (bad class/method name, load failure).</summary>
    public const string FatalPrefix = "@@FATAL ";
    /// <summary>Exit code used when a test exceeded its time limit and the remaining tests were skipped.</summary>
    public const int TimeoutExitCode = 3;
}
