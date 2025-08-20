using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Unfucked;

namespace GitHooks.Hooks;

/*
 * Finding XML elements in a project file is faster with regexes than with XML DOM, LINQ, or XPath, either using AOT or JIT:
 *
 * BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5371/22H2/2022Update)
 * AMD Ryzen 9 3900X, 1 CPU, 24 logical and 12 physical cores
 * .NET SDK 9.0.102
 *   [Host]   : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
 *   ShortRun : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
 *
 * Job=ShortRun  IterationCount=3  LaunchCount=1
 * WarmupCount=3
 *
 * | Method        | Toolchain | Mean      | Error     | StdDev    |
 * |-------------- |---------- |----------:|----------:|----------:|
 * | regexFound    | Default   |  1.404 us | 0.0226 us | 0.0012 us |
 * | regexNotFound | Default   |  2.064 us | 0.0136 us | 0.0007 us |
 * | linqFound     | Default   | 12.646 us | 2.5613 us | 0.1404 us |
 * | linqNotFound  | Default   | 13.131 us | 0.7208 us | 0.0395 us |
 * | domFound      | Default   | 40.090 us | 9.7396 us | 0.5339 us |
 * | domNotFound   | Default   | 41.667 us | 4.7237 us | 0.2589 us |
 * | xpathFound    | Default   | 15.645 us | 1.7563 us | 0.0963 us |
 * | xpathNotFound | Default   | 15.932 us | 0.5158 us | 0.0283 us |
 * | regexFound    | AOT       |  1.558 us | 0.0313 us | 0.0017 us |
 * | regexNotFound | AOT       |  2.382 us | 0.2314 us | 0.0127 us |
 * | linqFound     | AOT       | 16.725 us | 1.1036 us | 0.0605 us |
 * | linqNotFound  | AOT       | 16.980 us | 1.5630 us | 0.0857 us |
 * | domFound      | AOT       | 51.134 us | 1.7935 us | 0.0983 us |
 * | domNotFound   | AOT       | 52.419 us | 1.6967 us | 0.0930 us |
 * | xpathFound    | AOT       | 22.981 us | 0.7466 us | 0.0409 us |
 * | xpathNotFound | AOT       | 23.103 us | 0.9354 us | 0.0513 us |
 */
public class ILLinkRemover: PreCommitHook {

    private const string LOCK_FILENAME     = "packages.lock.json";
    private const string PACKAGE_TO_REMOVE = "Microsoft.NET.ILLink.Tasks";

    private static readonly Encoding              UTF8 = new UTF8Encoding(false, true);
    private static readonly string[]              SINGLE_FILE_ELEMENT_NAMES = ["PublishSingleFile", "PublishAOT"];
    private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.General) { WriteIndented = true, IndentSize = 2, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async Task<PreCommitHook.HookResult> run(IEnumerable<string> stagedFiles) {
        IEnumerable<string> stagedPackageLockFiles = stagedFiles.Where(filename => Path.GetFileName(filename).Equals(LOCK_FILENAME, StringComparison.OrdinalIgnoreCase));

        await Task.WhenAll(stagedPackageLockFiles.Select(async packageLockFilename => {
            Task<string> originalLockFileContentsTask = Git.readStagedFile(packageLockFilename);

            CancellationTokenSource projectReadCts = new();

            (string filename, string contents)? singleFileProject = await Tasks.FirstOrDefaultStruct(Directory
                    .EnumerateFiles(Path.GetDirectoryName(packageLockFilename)!, "*.csproj", SearchOption.TopDirectoryOnly)
                    .Select(async projectFilename =>
                        (filename: projectFilename, contents: await File.ReadAllTextAsync(projectFilename, UTF8, projectReadCts.Token))),
                result => linqHasElementWithText(result.contents, SINGLE_FILE_ELEMENT_NAMES, "true"), projectReadCts.Token);

            if (singleFileProject == default) {
                bool fileModified = false;

                JsonObject? packageLockObject = JsonSerializer.Deserialize<JsonObject>(await originalLockFileContentsTask, JSON_OPTIONS);
                foreach (KeyValuePair<string, JsonNode?> runtimeDependencies in packageLockObject?["dependencies"] as JsonObject ?? []) {
                    fileModified |= (runtimeDependencies.Value as JsonObject)?.Remove(PACKAGE_TO_REMOVE) ?? false;
                }

                if (fileModified) {
                    await using (FileStream lockFileWriteStream = File.Open(packageLockFilename, FileMode.Truncate, FileAccess.Write)) {
                        await JsonSerializer.SerializeAsync(lockFileWriteStream, packageLockObject, JSON_OPTIONS, CancellationToken.None);
                    }
                    await Git.stageFile(packageLockFilename);
                    Console.WriteLine($"Removed {PACKAGE_TO_REMOVE} from {packageLockFilename}");
                }
            }
        }));

        return PreCommitHook.HookResult.PROCEED_WITH_COMMIT;
    }

    private static bool linqHasElementWithText(string xmlDoc, IEnumerable<string> elementNames, string elementText) =>
        XDocument.Parse(xmlDoc)
            .Descendants()
            .IntersectBy(elementNames, element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
            .Any(element => element.FirstNode is XText text && elementText.Equals(text.Value, StringComparison.OrdinalIgnoreCase));

}