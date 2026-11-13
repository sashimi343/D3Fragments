// Summarizes Stryker.NET JSON reports with the criteria agreed for this repository.
//   dotnet run scripts/StrykerSummary.cs -- <report.json | directory>... [--threshold <0-100>]
// A directory is searched recursively and its newest mutation-report.json is used (e.g. a StrykerOutput folder).
// Exit code 1 when --threshold is given and any adjusted score is below it.
//
// Criteria
//   - Score = (Killed + Timeout) / (Killed + Timeout + Survived + NoCoverage), as Stryker computes it.
//   - ConfigureAwait(false) -> true mutants are equivalent (no synchronization context in tests) and are excluded
//     from the adjusted score. Stryker's ignore-methods cannot be used for this: it also ignores every mutant inside
//     the awaited call (e.g. removing "await unitOfWork.SaveChangesAsync(...)").
//   - Survivors in a static context (static initializers) are marked "static": Stryker cannot activate them,
//     so verify them by applying the mutation by hand. EF Core model building has the same problem but is not flagged.
#:property PublishAot=false
#:property TreatWarningsAsErrors=false
#:property EnableNETAnalyzers=false

using System.Globalization;
using System.Text.Json;

string? thresholdText = null;
var inputs = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--threshold" && i + 1 < args.Length)
    {
        thresholdText = args[++i];
    }
    else
    {
        inputs.Add(args[i]);
    }
}
if (inputs.Count == 0)
{
    Console.Error.WriteLine("Usage: dotnet run scripts/StrykerSummary.cs -- <report.json | directory>... [--threshold <0-100>]");
    return 2;
}
double? threshold = thresholdText is null ? null : double.Parse(thresholdText, CultureInfo.InvariantCulture);

var belowThreshold = false;
foreach (var input in inputs)
{
    var reportPath = File.Exists(input)
        ? input
        : Directory.Exists(input)
            ? Directory.EnumerateFiles(input, "mutation-report.json", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
    if (reportPath is null)
    {
        Console.Error.WriteLine($"No mutation-report.json found: {input}");
        return 2;
    }

    using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
    var report = document.RootElement;
    var projectRoot = report.TryGetProperty("projectRoot", out var rootElement) ? rootElement.GetString() : null;

    int killed = 0, survived = 0, noCoverage = 0, configureAwait = 0;
    var survivors = new List<string>();
    foreach (var file in report.GetProperty("files").EnumerateObject())
    {
        var lines = file.Value.GetProperty("source").GetString()!.Split('\n');
        var path = projectRoot is null ? file.Name : Path.GetRelativePath(projectRoot, file.Name);
        foreach (var mutant in file.Value.GetProperty("mutants").EnumerateArray())
        {
            var status = mutant.GetProperty("status").GetString();
            if (status is not ("Killed" or "Timeout" or "Survived" or "NoCoverage"))
            {
                continue;
            }

            var start = mutant.GetProperty("location").GetProperty("start");
            var end = mutant.GetProperty("location").GetProperty("end");
            var line = lines[start.GetProperty("line").GetInt32() - 1].TrimEnd('\r');
            if (IsConfigureAwaitEquivalent(mutant, line, start, end))
            {
                configureAwait++;
                continue;
            }

            switch (status)
            {
                case "Killed" or "Timeout":
                    killed++;
                    continue;
                case "Survived":
                    survived++;
                    break;
                default:
                    noCoverage++;
                    break;
            }

            var isStatic = mutant.TryGetProperty("static", out var staticElement) && staticElement.ValueKind == JsonValueKind.True;
            var replacement = mutant.TryGetProperty("replacement", out var r) ? r.GetString() : "";
            survivors.Add($"  [{status}] {path.Replace('\\', '/')}:{start.GetProperty("line").GetInt32()} {mutant.GetProperty("mutatorName").GetString()}"
                + $"{(isStatic ? " (static)" : "")} -> '{Truncate(replacement, 50)}' | {Truncate(line.Trim(), 100)}");
        }
    }

    var total = killed + survived + noCoverage;
    var adjusted = total == 0 ? 100 : 100.0 * killed / total;
    // Stryker's own score counts the ConfigureAwait mutants (they almost always survive)
    var raw = total + configureAwait == 0 ? 100 : 100.0 * killed / (total + configureAwait);

    Console.WriteLine($"== {projectRoot ?? reportPath}");
    Console.WriteLine($"Killed {killed}, Survived {survived}, NoCoverage {noCoverage}, ConfigureAwait equivalents excluded {configureAwait}");
    Console.WriteLine($"Score: {adjusted:F1}% (before excluding ConfigureAwait: {raw:F1}%)");
    foreach (var survivor in survivors)
    {
        Console.WriteLine(survivor);
    }
    belowThreshold |= threshold is { } t && adjusted < t;
}
return belowThreshold ? 1 : 0;

// The mutant replaces the "false" argument of ConfigureAwait(false) with "true"
static bool IsConfigureAwaitEquivalent(JsonElement mutant, string line, JsonElement start, JsonElement end)
{
    if (mutant.GetProperty("mutatorName").GetString() != "Boolean mutation" || start.GetProperty("line").GetInt32() != end.GetProperty("line").GetInt32())
    {
        return false;
    }
    // columns are 1-based and the end is exclusive
    var from = start.GetProperty("column").GetInt32() - 1;
    var to = end.GetProperty("column").GetInt32() - 1;
    return from >= 0 && to <= line.Length && line[from..to] == "false" && line[..from].TrimEnd().EndsWith("ConfigureAwait(", StringComparison.Ordinal);
}

static string Truncate(string? text, int length) => text is null ? "" : text.Length <= length ? text : text[..length];
