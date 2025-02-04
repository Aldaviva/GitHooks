using GitHooks;
using GitHooks.Hooks;

/*Console.WriteLine($"cmd: {string.Join(' ', Environment.GetCommandLineArgs())}");
Console.WriteLine($"cwd: {Environment.CurrentDirectory}");
Console.WriteLine(
    $"env: {string.Join("\n     ", Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(entry => entry.Key.ToString(), StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"))}");*/

ICollection<PreCommitHook> precommitHooks = [
    new FixMeBlocker(),
    new ILLinkRemover()
];

string[] stagedFiles = await Git.getStagedFiles();

PreCommitHook.HookResult[] hookResults = await Task.WhenAll(precommitHooks.Select(hook => hook.run(stagedFiles)));

return hookResults.Any(result => result == PreCommitHook.HookResult.ABORT_COMMIT) ? 1 : 0;