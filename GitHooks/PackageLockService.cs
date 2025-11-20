using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GitHooks;

public class PackageLockService {

    private const string LOCK_FILENAME = "packages.lock.json";

    public static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.General) { WriteIndented = true, IndentSize = 2, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly ConcurrentDictionary<string, Task<JsonObject>> lockFileContentsByRepoRelativeFilename = new();

    public static bool isPackageLockFile(string filePath) => Path.GetFileName(filePath).Equals(LOCK_FILENAME, StringComparison.OrdinalIgnoreCase);

    public async Task<JsonObject> getLockFileContents(string repoRelativeFilename) {
        TaskCompletionSource<JsonObject>? taskCompletionSource = null;

        Task<JsonObject> result = lockFileContentsByRepoRelativeFilename.GetOrAdd(repoRelativeFilename, _ => {
            taskCompletionSource = new TaskCompletionSource<JsonObject>();
            return taskCompletionSource.Task;
        }, out bool added);

        if (added) {
            taskCompletionSource!.TrySetResult(await loadLockFile(repoRelativeFilename));
        }

        return await result;
    }

    private static async Task<JsonObject> loadLockFile(string repoRelativeFilename) =>
        JsonSerializer.Deserialize<JsonObject>(await Git.readStagedFile(repoRelativeFilename), JSON_OPTIONS)!;

}