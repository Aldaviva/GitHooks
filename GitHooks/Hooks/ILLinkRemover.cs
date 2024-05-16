using GitHooks.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHooks.Hooks;

//TODO: make this allow ILLink in package lock files when the csproj file in the same directory has a direct dependency on that ILLink package, or maybe if PublishAot is true
public partial class ILLinkRemover: PrecommitHook {

    private const string NEEDLE        = "Microsoft.NET.ILLink.Tasks";
    private const string LOCK_FILENAME = "packages.lock.json";

    private static readonly Encoding UTF8 = new UTF8Encoding(false, true);

    [GeneratedRegex(@"""Microsoft\.NET\.ILLink\.Tasks""\s*:\s*{.*?},?\s*", RegexOptions.Singleline)]
    private static partial Regex ilLinkPattern();

    [GeneratedRegex(@"<\s*PublishAot\s*>\s*true\s*</\s*PublishAot\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex publishAotPattern();

    public async Task<PrecommitHook.HookResult> run(IEnumerable<string> stagedFiles) {
        IEnumerable<string> stagedPackageLockFiles = stagedFiles.Where(filename => Path.GetFileName(filename).Equals(LOCK_FILENAME, StringComparison.InvariantCultureIgnoreCase));

        await Task.WhenAll(stagedPackageLockFiles.Select(async packageLockFilename => {
            /*
             * For some reason, using a FileStream while AOT compiling this program usually throws
             * "System.IO.IOException: The process cannot access the file 'C:\Users\Ben\Documents\Projects\MailSender.cs\MailSender-NetCore\packages.lock.json' because it is being used by another process."
             * in the FileStream constructor, even if no other process has a handle on that file.
             *
             * Workaround: replaced single FileStream instance with separate calls to File.ReadAllTextAsync and File.WriteAllTextAsync
             */
            Task<string> originalFileContentsTask = File.ReadAllTextAsync(packageLockFilename, UTF8);

            CancellationTokenSource projectReadCts = new();

            (string filename, string contents) aotProject = await Task2.firstOrDefault(Directory.EnumerateFiles(Path.GetDirectoryName(packageLockFilename)!, "*.csproj", SearchOption.TopDirectoryOnly)
                    .Select(async projectFilename =>
                        (filename: projectFilename, contents: await File.ReadAllTextAsync(projectFilename, UTF8, projectReadCts.Token))),
                task => publishAotPattern().IsMatch(task.Result.contents), projectReadCts);

            if (aotProject == default) {
                bool   fileModified         = false;
                string originalFileContents = await originalFileContentsTask;
                string modifiedFileContents = ilLinkPattern().Replace(originalFileContents, _ => {
                    fileModified = true;
                    return string.Empty;
                });

                if (fileModified) {
                    await File.WriteAllTextAsync(packageLockFilename, modifiedFileContents, UTF8, CancellationToken.None);
                    await Git.stageFile(packageLockFilename);
                    Console.WriteLine($"Removed {NEEDLE} from {packageLockFilename}");
                }
            }
        }));

        return PrecommitHook.HookResult.PROCEED_WITH_COMMIT;
    }

}