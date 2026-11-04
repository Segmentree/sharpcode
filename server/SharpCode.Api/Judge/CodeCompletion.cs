using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SharpCode.Api.Judge;

public sealed record CompletionItemDto(string Label, string Kind, string? Detail);

public sealed record CompletionResponse(int From, IReadOnlyList<CompletionItemDto> Items);

/// <summary>
/// IntelliSense-style completions backed by Roslyn. A single in-memory workspace is created once; each request
/// forks an immutable document with the user's text, so it is cheap and thread-safe.
/// </summary>
public sealed class CodeCompletion
{
    private const int MaxItems = 400;
    private readonly Document _template;

    public CodeCompletion()
    {
        var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Submission", "Submission",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                metadataReferences: Compiler.References.Value);
        var project = workspace.AddProject(projectInfo);
        workspace.AddDocument(project.Id, "GlobalUsings.cs", SourceText.From(Compiler.GlobalUsings));
        _template = workspace.AddDocument(project.Id, "Solution.cs", SourceText.From(""));
    }

    public async Task<CompletionResponse> CompleteAsync(string code, int position, CancellationToken ct)
    {
        position = Math.Clamp(position, 0, code.Length);
        var document = _template.WithText(SourceText.From(code));
        var service = CompletionService.GetService(document)
            ?? throw new InvalidOperationException("Roslyn completion service is unavailable.");

        var list = await service.GetCompletionsAsync(document, position, cancellationToken: ct);
        if (list.ItemsList.Count == 0) return new CompletionResponse(position, []);

        var span = list.Span;
        var prefix = code.Substring(span.Start, Math.Min(span.Length, position - span.Start));

        var items = list.ItemsList
            .Where(i => prefix.Length == 0 || i.FilterText.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => prefix.Length == 0 || i.FilterText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(i => i.Tags.Contains("ExtensionMethod") ? 1 : 0) // a type's own members before LINQ-style extensions
            .ThenBy(i => i.SortText, StringComparer.Ordinal)
            .GroupBy(i => i.DisplayText)
            .Select(g => g.First())
            .Take(MaxItems)
            .Select(i => new CompletionItemDto(i.DisplayText, MapKind(i.Tags), i.InlineDescription is { Length: > 0 } d ? d : null))
            .ToList();

        return new CompletionResponse(span.Start, items);
    }

    // Roslyn's WellKnownTags constants are internal; these are their string values.
    private static string MapKind(System.Collections.Immutable.ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case "Method" or "ExtensionMethod": return "method";
                case "Property" or "Event": return "property";
                case "Field" or "Local" or "Parameter" or "RangeVariable": return "variable";
                case "Constant" or "EnumMember": return "constant";
                case "Class" or "Structure" or "Delegate": return "class";
                case "Interface": return "interface";
                case "Enum": return "enum";
                case "Namespace": return "namespace";
                case "Keyword": return "keyword";
                case "Snippet": return "text";
            }
        }
        return "text";
    }
}
