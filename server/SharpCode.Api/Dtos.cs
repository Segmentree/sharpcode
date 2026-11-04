using System.ComponentModel.DataAnnotations;
using SharpCode.Api.Data;
using SharpCode.Api.Judge;

namespace SharpCode.Api;

public sealed record ProblemSummaryDto(int Id, string Slug, string Title, Difficulty Difficulty, ProblemCategory Category,
    int TestCaseCount, bool Solved);

public sealed record TestCaseDto(int Id, string Input, string Expected, bool IsHidden, int Order);

public sealed record TranslationDto(string Title, string Description);

public sealed record ProblemDto(int Id, string Slug, string Title, string Description, Difficulty Difficulty,
    ProblemCategory Category, string StarterCode, string ClassName, string MethodName, int TimeLimitMs,
    IReadOnlyList<TestCaseDto> TestCases, Dictionary<string, TranslationDto>? Translations);

public sealed record TestCaseInput(
    [property: Required] string Input,
    [property: Required] string Expected,
    bool IsHidden = false);

public sealed record ProblemInput(
    [property: Required, MaxLength(200)] string Title,
    string? Slug,
    [property: Required] string Description,
    Difficulty Difficulty,
    ProblemCategory Category,
    [property: Required] string StarterCode,
    [property: Required] string MethodName,
    string? ClassName,
    int? TimeLimitMs,
    List<TestCaseInput>? TestCases,
    Dictionary<string, TranslationDto>? Translations);

public sealed record CodeInput([property: Required] string Code);

public sealed record CompleteInput(string? Code, int Position);

public sealed record SandboxInput(string? Code, string? Stdin);

public sealed record SubmissionDto(int Id, int ProblemId, Verdict Verdict, int PassedCount, int TotalCount,
    DateTime CreatedAt, string Code, JudgeResult? Result);

public static class Mapping
{
    public const int VisibleCases = 5;
    public const int HiddenCases = 20;

    public static TestCaseDto ToDto(this TestCase t, bool includeHiddenDetails) =>
        new(t.Id, includeHiddenDetails || !t.IsHidden ? t.Input : "", includeHiddenDetails || !t.IsHidden ? t.Expected : "",
            t.IsHidden, t.Order);

    /// <param name="lang">Locale used for title/description; English (the base fields) when no translation exists.</param>
    public static ProblemDto ToDto(this Problem p, bool includeHiddenDetails, string? lang, bool includeTranslations = false)
    {
        var tr = Localize(p, lang);
        return new ProblemDto(p.Id, p.Slug, tr.Title, tr.Description, p.Difficulty, p.Category, p.StarterCode,
            p.ClassName, p.MethodName, p.TimeLimitMs,
            p.TestCases.OrderBy(t => t.Order).ThenBy(t => t.Id).Select(t => t.ToDto(includeHiddenDetails)).ToList(),
            includeTranslations
                ? p.Translations.ToDictionary(t => t.Locale, t => new TranslationDto(t.Title, t.Description))
                : null);
    }

    public static (string Title, string Description) Localize(Problem p, string? lang)
    {
        if (lang is null or "en") return (p.Title, p.Description);
        var t = p.Translations.FirstOrDefault(t => t.Locale.Equals(lang, StringComparison.OrdinalIgnoreCase));
        return t is null ? (p.Title, p.Description) : (t.Title, t.Description);
    }

    public static string Slugify(string title)
    {
        var chars = title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
