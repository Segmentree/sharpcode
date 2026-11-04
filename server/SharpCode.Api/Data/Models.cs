namespace SharpCode.Api.Data;

public enum Difficulty { Easy = 1, Medium = 2, Hard = 3 }

public enum ProblemCategory { School = 1, Gym = 2 }

public sealed class Problem
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    /// <summary>Markdown statement shown to the user.</summary>
    public required string Description { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public ProblemCategory Category { get; set; } = ProblemCategory.Gym;
    /// <summary>Code pre-filled in the editor.</summary>
    public required string StarterCode { get; set; }
    /// <summary>Class the judge instantiates. Defaults to "Solution".</summary>
    public string ClassName { get; set; } = "Solution";
    /// <summary>Public method the judge invokes with the test-case arguments.</summary>
    public required string MethodName { get; set; }
    /// <summary>Per test case wall-clock limit enforced by the judge.</summary>
    public int TimeLimitMs { get; set; } = 2000;
    /// <summary>Documentation topic slugs (client-side content) useful for this problem, stored as a JSON array.</summary>
    public string DocLinksJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TestCase> TestCases { get; set; } = [];
    /// <summary>Title/description in other languages. Title and Description on the problem itself are English.</summary>
    public List<ProblemTranslation> Translations { get; set; } = [];
}

public sealed class ProblemTranslation
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public Problem? Problem { get; set; }
    /// <summary>Two-letter language code, e.g. "es".</summary>
    public required string Locale { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
}

public sealed class TestCase
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public Problem? Problem { get; set; }
    /// <summary>JSON array of arguments, e.g. "[[2,7,11,15], 9]".</summary>
    public required string Input { get; set; }
    /// <summary>JSON expected return value, e.g. "[0,1]".</summary>
    public required string Expected { get; set; }
    /// <summary>Hidden cases run on submit but their input/expected output is not revealed.</summary>
    public bool IsHidden { get; set; }
    public int Order { get; set; }
}

public enum Verdict { Accepted, WrongAnswer, CompileError, RuntimeError, TimeLimitExceeded, JudgeError }

public sealed class Submission
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public Problem? Problem { get; set; }
    public required string Code { get; set; }
    public Verdict Verdict { get; set; }
    public int PassedCount { get; set; }
    public int TotalCount { get; set; }
    /// <summary>Serialized JudgeResult so the UI can replay old feedback.</summary>
    public required string ResultJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
