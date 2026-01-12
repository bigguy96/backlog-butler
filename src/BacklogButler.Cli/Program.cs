using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var options = Options.Parse(args);

if (options.ShowHelp)
{
    PrintHelp();
    return 0;
}

// Resolve from env vars if not provided as args
options = options with
{
    Org = string.IsNullOrWhiteSpace(options.Org) ? Environment.GetEnvironmentVariable("ADO_ORG") ?? "" : options.Org,
    Project = string.IsNullOrWhiteSpace(options.Project) ? Environment.GetEnvironmentVariable("ADO_PROJECT") ?? "" : options.Project,
    Pat = string.IsNullOrWhiteSpace(options.Pat) ? Environment.GetEnvironmentVariable("ADO_PAT") ?? "" : options.Pat,
};

var apply = options.Apply;
var dryRun = !apply;

Console.WriteLine("Backlog Butler 🧹");
Console.WriteLine($"Mode: {(dryRun ? "Dry-run" : "Apply")}");
Console.WriteLine();

if (dryRun)
{
    Console.WriteLine("No changes will be made.");
    Console.WriteLine("Use --apply to perform updates (when write actions are added).");
    Console.WriteLine();
}

// Validate required ADO settings (for read-only tag listing)
if (string.IsNullOrWhiteSpace(options.Org) ||
    string.IsNullOrWhiteSpace(options.Project) ||
    string.IsNullOrWhiteSpace(options.Pat))
{
    Console.WriteLine("Missing Azure DevOps settings.");
    Console.WriteLine("Provide via args: --org --project --pat");
    Console.WriteLine("Or via env vars: ADO_ORG, ADO_PROJECT, ADO_PAT");
    Console.WriteLine();
    PrintHelp();
    return 2;
}

// Normalize org URL
var org = options.Org.TrimEnd('/');

try
{
    using var http = CreateAdoHttpClient(org, options.Pat);

    // Read-only: list all tags for the project
    var tags = await ListProjectTagsAsync(http, options.Project);

    Console.WriteLine($"Azure DevOps Org: {org}");
    Console.WriteLine($"Project: {options.Project}");
    Console.WriteLine($"Tags found: {tags.Count}");
    Console.WriteLine();

    foreach (var t in tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        Console.WriteLine($"- {t}");

    return 0;
}
catch (HttpRequestException ex)
{
    Console.WriteLine("HTTP error while calling Azure DevOps:");
    Console.WriteLine(ex.Message);
    return 10;
}
catch (Exception ex)
{
    Console.WriteLine("Unexpected error:");
    Console.WriteLine(ex);
    return 11;
}

static HttpClient CreateAdoHttpClient(string orgUrl, string pat)
{
    var http = new HttpClient
    {
        BaseAddress = new Uri(orgUrl + "/")
    };

    // PAT uses Basic auth: username blank, password = PAT
    var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    return http;
}

static async Task<List<string>> ListProjectTagsAsync(HttpClient http, string project)
{
    // GET https://dev.azure.com/{org}/{project}/_apis/wit/tags?api-version=7.1
    var url = $"{Uri.EscapeDataString(project)}/_apis/wit/tags?api-version=7.1";

    using var resp = await http.GetAsync(url);
    var body = await resp.Content.ReadAsStringAsync();

    if (!resp.IsSuccessStatusCode)
        throw new Exception($"ADO API call failed: {(int)resp.StatusCode} {resp.StatusCode}\n{body}");

    using var doc = JsonDocument.Parse(body);

    // Response shape: { "count": n, "value": [ { "name": "PFTR", ... }, ... ] }
    var tags = new List<string>();
    if (doc.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in value.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    tags.Add(name);
            }
        }
    }

    return tags;
}

static void PrintHelp()
{
    Console.WriteLine("""
Usage:
  BacklogButler.Cli [--help] [--org <url>] [--project <name>] [--pat <token>] [--apply]

Examples (env vars recommended):
  # PowerShell
  $env:ADO_ORG="https://dev.azure.com/yourorg"
  $env:ADO_PROJECT="YourProject"
  $env:ADO_PAT="YOUR_PAT"
  dotnet run --project src/BacklogButler.Cli

Examples (args):
  dotnet run --project src/BacklogButler.Cli -- --org https://dev.azure.com/yourorg --project YourProject --pat YOUR_PAT

Options:
  --help        Show this help
  --org         Azure DevOps org URL (or env ADO_ORG)
  --project     Azure DevOps project name (or env ADO_PROJECT)
  --pat         Personal Access Token (or env ADO_PAT)
  --apply       Apply changes (future write actions). Default is dry-run.

Env vars:
  ADO_ORG, ADO_PROJECT, ADO_PAT
""");
}

public sealed record Options(
    bool ShowHelp,
    bool Apply,
    string Org,
    string Project,
    string Pat)
{
    public static Options Parse(string[] args)
    {
        bool HasFlag(string flag) =>
            args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        string GetValue(string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
            }
            return "";
        }

        return new Options(
            ShowHelp: HasFlag("--help") || HasFlag("-h") || HasFlag("/?"),
            Apply: HasFlag("--apply"),
            Org: GetValue("--org"),
            Project: GetValue("--project"),
            Pat: GetValue("--pat")
        );
    }
}