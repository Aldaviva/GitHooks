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
- Checks staged .NET package lock files for `Microsoft.NET.ILLink.Tasks`, which is automatically added by Visual Studio and causes package restoration in locked mode to fail because it's not a real dependency of your project.
    - If it finds any, it automatically removes this dependency from `packages.lock.json` so that `dotnet restore --locked-mode` will succeed. This does not break Visual Studio either, which will either ignore it or try to add it again later. The commit continues automatically.
    - This dependency is preserved if the project publishes to a single file, for example if `PublishSingleFile` or `PublishAot` are set to `true` in the `.csproj` file.

## Requirements
- [Git](https://git-scm.com/downloads)

## Installation
1. Download [`pre-commit.exe`](https://github.com/Aldaviva/GitHooks/releases/latest/download/pre-commit.exe) from the [latest release](https://github.com/Aldaviva/GitHooks/releases/latest).
1. Save it to a Git hooks directory, depending on the scope you want it to apply to.
    - **One repository:** `<repo>/git/hooks/pre-commit.exe`
    - **All repositories for a user:** any directory, then run `git config --global core.hookspath "<directory>"`
    - **All repositories on a computer:** any directory, then run `git config --system core.hookspath "<directory>"`

## Usage
Just run `git commit` the way you normally would. If it commits successfully, there were no fatal errors.

### Testing
If you want to test the hook without committing anything, you can run
```sh
git hook run pre-commit --no-ignore-missing
```