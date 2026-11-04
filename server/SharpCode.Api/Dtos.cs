using System.ComponentModel.DataAnnotations;
using SharpCode.Api.Data;
using SharpCode.Api.Judge;

namespace SharpCode.Api;

public sealed record ProblemSummaryDto(int Id, string Slug, string Title, Difficulty Difficulty, int TestCaseCount,
    bool Solved);

public sealed record TestCaseDto(int Id, string Input, string Expected, bool IsHidden, int Order);

public sealed record ProblemDto(int Id, string Slug, string Title, string Description, Difficulty Difficulty,
    string StarterCode, string ClassName, string MethodName, int TimeLimitMs, IReadOnlyList<TestCaseDto> TestCases);

public sealed record TestCaseInput(
    [property: Required] string Input,
    [property: Required] string Expected,
    bool IsHidden = false);

public sealed record ProblemInput(
    [property: Required, MaxLength(200)] string Title,
    string? Slug,
    [property: Required] string Description,
    Difficulty Difficulty,
    [property: Required] string StarterCode,
    [property: Required] string MethodName,
    string? ClassName,
    int? TimeLimitMs,
    List<TestCaseInput>? TestCases);

public sealed record CodeInput([property: Required] string Code);

public sealed record SubmissionDto(int Id, int ProblemId, Verdict Verdict, int PassedCount, int TotalCount,
    DateTime CreatedAt, string Code, JudgeResult? Result);

public static class Mapping
{
    public static TestCaseDto ToDto(this TestCase t, bool includeHiddenDetails) =>
        new(t.Id, includeHiddenDetails || !t.IsHidden ? t.Input : "", includeHiddenDetails || !t.IsHidden ? t.Expected : "",
            t.IsHidden, t.Order);

    public static ProblemDto ToDto(this Problem p, bool includeHiddenDetails) =>
        new(p.Id, p.Slug, p.Title, p.Description, p.Difficulty, p.StarterCode, p.ClassName, p.MethodName, p.TimeLimitMs,
            p.TestCases.OrderBy(t => t.Order).ThenBy(t => t.Id).Select(t => t.ToDto(includeHiddenDetails)).ToList());

    public static string Slugify(string title)
    {
        var chars = title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
