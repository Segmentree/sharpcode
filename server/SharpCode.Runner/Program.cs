using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SharpCode.Runner.Contracts;

// Usage: SharpCode.Runner <assembly.dll> <className> <methodName> <tests.json> <timeLimitMs>
// Executes each test case against the compiled user assembly and prints one result line per test.
// This process is meant to be disposable: the API kills it when it exceeds the time limit.

var realOut = Console.Out;
Console.SetIn(TextReader.Null);

if (args.Length != 5 || !int.TryParse(args[4], out var timeLimitMs))
{
    realOut.WriteLine(RunnerProtocol.FatalPrefix + "expected 5 arguments: <dll> <class> <method> <tests.json> <timeLimitMs>");
    return 2;
}

var (dllPath, className, methodName, testsPath) = (args[0], args[1], args[2], args[3]);
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };

List<RunnerTestCase> tests;
MethodInfo method;
object? instance;
try
{
    tests = JsonSerializer.Deserialize<List<RunnerTestCase>>(File.ReadAllText(testsPath), jsonOptions) ?? [];
    var assembly = Assembly.LoadFrom(dllPath);
    var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className || t.FullName == className)
        ?? throw new InvalidOperationException($"Class '{className}' was not found. Keep the class named '{className}'.");
    method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Public method '{methodName}' was not found on class '{className}'.");
    instance = method.IsStatic ? null : Activator.CreateInstance(type);
}
catch (Exception ex)
{
    realOut.WriteLine(RunnerProtocol.FatalPrefix + Flatten(ex));
    return 1;
}

var parameters = method.GetParameters();

foreach (var test in tests)
{
    // User code runs on its own thread so a runaway test can be reported precisely. A thread cannot be
    // aborted on .NET, so after reporting the time-out this process simply exits; the API treats exit
    // code TimeoutExitCode as "the remaining tests never ran".
    RunnerTestResult? result = null;
    var worker = new Thread(() => result = Execute(test), 64 * 1024 * 1024) { IsBackground = true };
    worker.Start();
    if (!worker.Join(timeLimitMs) || result is null)
    {
        Console.SetOut(realOut);
        result = new RunnerTestResult { Id = test.Id, Passed = false, Error = $"Time limit exceeded ({timeLimitMs} ms).", DurationMs = timeLimitMs };
        realOut.WriteLine(RunnerProtocol.ResultPrefix + JsonSerializer.Serialize(result));
        realOut.Flush();
        return RunnerProtocol.TimeoutExitCode;
    }
    realOut.WriteLine(RunnerProtocol.ResultPrefix + JsonSerializer.Serialize(result));
    realOut.Flush();
}
return 0;

RunnerTestResult Execute(RunnerTestCase test)
{
    var stdout = new StringWriter();
    Console.SetOut(stdout);
    Console.SetError(stdout);
    var sw = Stopwatch.StartNew();
    try
    {
        var argsNode = JsonNode.Parse(test.Input) as JsonArray
            ?? throw new InvalidOperationException("Test input must be a JSON array of arguments.");
        if (argsNode.Count != parameters.Length)
            throw new InvalidOperationException(
                $"Method '{methodName}' takes {parameters.Length} argument(s) but the test provides {argsNode.Count}.");

        var invokeArgs = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            invokeArgs[i] = argsNode[i] is null
                ? null
                : JsonSerializer.Deserialize(argsNode[i]!.ToJsonString(), parameters[i].ParameterType, jsonOptions);

        var returned = method.Invoke(instance, invokeArgs);
        sw.Stop();

        var actualJson = method.ReturnType == typeof(void)
            ? "null"
            : JsonSerializer.Serialize(returned, method.ReturnType, jsonOptions);

        var passed = JsonEquals(JsonNode.Parse(actualJson), JsonNode.Parse(test.Expected));
        return new RunnerTestResult
        {
            Id = test.Id, Passed = passed, Actual = actualJson,
            Stdout = Truncate(stdout.ToString()), DurationMs = sw.Elapsed.TotalMilliseconds
        };
    }
    catch (Exception ex)
    {
        sw.Stop();
        var inner = ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException! : ex;
        return new RunnerTestResult
        {
            Id = test.Id, Passed = false, Error = Flatten(inner),
            Stdout = Truncate(stdout.ToString()), DurationMs = sw.Elapsed.TotalMilliseconds
        };
    }
    finally
    {
        Console.SetOut(realOut);
    }
}

static bool JsonEquals(JsonNode? a, JsonNode? b)
{
    if (a is null || b is null) return a is null && b is null;
    switch (a, b)
    {
        case (JsonArray xa, JsonArray xb):
            return xa.Count == xb.Count && xa.Zip(xb).All(p => JsonEquals(p.First, p.Second));
        case (JsonObject oa, JsonObject ob):
            return oa.Count == ob.Count && oa.All(kv => ob.ContainsKey(kv.Key) && JsonEquals(kv.Value, ob[kv.Key]));
        case (JsonValue va, JsonValue vb):
            if (va.TryGetValue<double>(out var da) && vb.TryGetValue<double>(out var db))
                return Math.Abs(da - db) <= 1e-9 * Math.Max(1, Math.Max(Math.Abs(da), Math.Abs(db)));
            return JsonNode.DeepEquals(va, vb);
        default:
            return false;
    }
}

static string Flatten(Exception ex)
{
    var msg = $"{ex.GetType().Name}: {ex.Message}";
    if (ex is ReflectionTypeLoadException rtle)
        msg += " " + string.Join("; ", rtle.LoaderExceptions.Select(e => e?.Message));
    return msg;
}

static string Truncate(string s) => s.Length <= 4000 ? s : s[..4000] + "\n…(truncated)";
