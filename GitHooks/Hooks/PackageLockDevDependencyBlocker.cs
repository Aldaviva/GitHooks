using System.Text.Json;
using System.Text.Json.Nodes;

namespace GitHooks.Hooks;

public class PackageLockDevDependencyBlocker(PackageLockService packageLockService): PreCommitHook {

    private static readonly string PACKAGE_CACHE_DIR = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages");

    private static readonly string[] ALLOWED_SOURCE_PREFIXES = [
        "https://api.nuget.org/",
        "https://nuget.pkg.github.com/",
        "https://www.myget.org/",
        Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft SDKs\NuGetPackages")
    ];

    public async Task<PreCommitHook.HookResult> run(IEnumerable<string> stagedFiles) =>
        (await Task.WhenAll(stagedFiles.Where(PackageLockService.isPackageLockFile).Select(async packageLockFilename => {
                JsonObject lockFileContents = await packageLockService.getLockFileContents(packageLockFilename);

                IEnumerable<(string name, string version)> resolvedDependencies = lockFileContents["dependencies"]?.AsObject().SelectMany(tfm =>
                    tfm.Value?.AsObject()
                        .Where(dependency => dependency.Value!["type"]?.GetValue<string>() != "Project")
                        .Select(package => (name: package.Key, version: package.Value!["resolved"]!.GetValue<string>())) ?? []) ?? [];

                return (await Task.WhenAll(resolvedDependencies.Select(async dependency => {
                    try {
                        await using FileStream metadataStream =
                            File.OpenRead(Path.Combine(PACKAGE_CACHE_DIR, dependency.name.ToLowerInvariant(), dependency.version.ToLowerInvariant(), ".nupkg.metadata"));

                        JsonObject metadataObject   = (await JsonSerializer.DeserializeAsync<JsonObject>(metadataStream))!;
                        string     dependencySource = metadataObject["source"]!.GetValue<string>();

                        if (ALLOWED_SOURCE_PREFIXES.Any(prefix => dependencySource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) {
                            return true;
                        } else {
                            Console.WriteLine(
                                "{0} depends on {1} {2}, which is a development version from {3}. To avoid build failures, restore this project, or deploy that package version to NuGet Gallery.",
                                packageLockFilename, dependency.name, dependency.version, dependencySource);
                            return false;
                        }
                    } catch (FileNotFoundException) {
                        return true;
                    }
                }))).All(isAllowed => isAllowed);
            }))
        ).All(isAllowed => isAllowed) ? PreCommitHook.HookResult.PROCEED_WITH_COMMIT : PreCommitHook.HookResult.ABORT_COMMIT;

}