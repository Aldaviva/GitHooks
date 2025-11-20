<img src="GitHooks/icon.ico" height="24" alt="logo" /> GitHooks
===

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/GitHooks/dotnet.yml?branch=master&logo=github)](https://github.com/Aldaviva/GitHooks/actions/workflows/dotnet.yml)

Custom logic that runs right before you create a Git commit.

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2" bullets="-,1.,-" -->

- [Checks performed](#checks-performed)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)

<!-- /MarkdownTOC -->

## Checks performed
- Checks for `FIXME` in any of the staged text files, so that you don't accidentally commit any code that you don't want to.
    - If it finds any, it stops the commit and tells you which file and line number it was found in, so you can fix the code.
- Checks staged [.NET package lock files](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies) for 
    - `Microsoft.NET.ILLink.Tasks`, which is automatically added by Visual Studio and causes package restoration in locked mode to fail because it's not a real dependency of your project.
        - If it finds any, it automatically removes this dependency from `packages.lock.json` so that `dotnet restore --locked-mode` will succeed. This does not break Visual Studio either, which will either ignore it or try to add it again later. The commit continues automatically.
        - This dependency is preserved if the project publishes to a single file, for example if `PublishSingleFile` or `PublishAot` are set to `true` in the `.csproj` file.
    - Locally installed development versions of dependencies that are likely to fail CI builds, such as those installed by [workspace resolution](https://github.com/Aldaviva/DependencyHierarchy/tree/master/WorkspaceResolution).
        - If it finds any, it aborts the commit and tells you which development dependency needs to be restored or published to a package version in NuGet Gallery or GitHub Packages.

## Requirements
- Operating system: Windows (x64 or arm64) or Linux (x64, arm, or arm64)
- [Git](https://git-scm.com/downloads)
- [.NET Runtime](https://dotnet.microsoft.com/en-us/download) 10 or later

## Installation
1. Download [`GitHooks.zip`](https://github.com/Aldaviva/GitHooks/releases/latest/download/GitHooks.zip) from the [latest release](https://github.com/Aldaviva/GitHooks/releases/latest).
1. Locate the `pre-commit` executable in the subdirectory of the ZIP file which corresponds to your operating system and CPU architecture (e.g. `win-x64\pre-commit.exe` for Windows x64).
1. Extract that `pre-commit` executable to a Git hooks directory, depending on the scope you want it to apply to.
    - **One repository:** extract it to `<repo>/git/hooks/pre-commit`
    - **All repositories for a user:** extract it to any directory, then run `git config --global core.hookspath "<dir>"`
    - **All repositories for all users:** extract it to any directory, then run `git config --system core.hookspath "<dir>"`

## Usage
Just run `git commit` the way you normally would. If it commits successfully, there were no errors.

Otherwise, it will abort the commit and print a message showing why it stopped and how to proceed. This generally involves fixing whatever you commented with `FIXME`, removing that comment, running `git add` on that fixed file, and then calling `git commit` again with your original commit message.

### Testing
If you want to run this hook without committing anything, you can call
```sh
git hook run pre-commit --no-ignore-missing
```