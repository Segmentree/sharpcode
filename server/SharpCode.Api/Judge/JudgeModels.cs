using SharpCode.Api.Data;

namespace SharpCode.Api.Judge;

public sealed record TestCaseResult(
    int TestCaseId,
    bool Passed,
    bool IsHidden,
    string? Input,
    string? Expected,
    string? Actual,
    string? Stdout,
    string? Error,
    double DurationMs);

public sealed record JudgeResult(
    Verdict Verdict,
    string Message,
    IReadOnlyList<CompileDiagnostic> Diagnostics,
    IReadOnlyList<TestCaseResult> Results,
    int PassedCount,
    int TotalCount,
    double TotalDurationMs);
