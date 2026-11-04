using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpCode.Api.Judge;

public sealed record CompileDiagnostic(int Line, int Column, string Code, string Message, bool IsError);

public sealed record CompileOutcome(byte[]? Assembly, IReadOnlyList<CompileDiagnostic> Diagnostics)
{
    public bool Success => Assembly is not null;
}

/// <summary>Compiles a user's C# source into an in-memory assembly with Roslyn.</summary>
public sealed class Compiler
{
    // Implicit usings so solutions read like LeetCode snippets; explicit duplicates in user code are harmless.
    public const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Text;
        """;

    public static readonly Lazy<ImmutableArray<MetadataReference>> References = new(LoadReferences);

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        // Reference the base class library of the runtime we are running on. The runner process runs on the
        // same runtime, so everything resolvable here is resolvable there.
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return tpa
            .Where(p =>
            {
                var name = Path.GetFileNameWithoutExtension(p);
                return name is "mscorlib" or "netstandard" or "Microsoft.CSharp" || name.StartsWith("System.");
            })
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToImmutableArray();
    }

    public CompileOutcome Compile(string source, string assemblyName)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(source, parseOptions, path: "Solution.cs"),
            CSharpSyntaxTree.ParseText(GlobalUsings, parseOptions, path: "GlobalUsings.cs"),
        };
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            nullableContextOptions: NullableContextOptions.Enable,
            allowUnsafe: false,
            // Hide warnings for unused usings etc. so feedback stays focused.
            specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic>
            {
                ["CS8019"] = ReportDiagnostic.Suppress, // unnecessary using
                ["CS1701"] = ReportDiagnostic.Suppress,
                ["CS1702"] = ReportDiagnostic.Suppress,
            });

        var compilation = CSharpCompilation.Create(assemblyName, trees, References.Value, options);
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        var diagnostics = result.Diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning && d.Location.SourceTree?.FilePath != "GlobalUsings.cs")
            .OrderByDescending(d => d.Severity).ThenBy(d => d.Location.SourceSpan.Start)
            .Select(d =>
            {
                var pos = d.Location.GetLineSpan().StartLinePosition;
                return new CompileDiagnostic(pos.Line + 1, pos.Character + 1, d.Id, d.GetMessage(),
                    d.Severity == DiagnosticSeverity.Error);
            })
            .ToList();

        return new CompileOutcome(result.Success ? ms.ToArray() : null, diagnostics);
    }
}
