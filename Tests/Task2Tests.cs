/*using FluentAssertions;
using GitHooks.Tasks;

namespace Tests;

public class Task2Tests {

    [Fact]
    public async Task anyEnumerableTrue() {
        bool any = await Task2.any([Task.CompletedTask, new TaskCompletionSource().Task], task => true);
        any.Should().BeTrue();
    }

    [Fact]
    public async Task anyEnumerableFalse() {
        bool any = await Task2.any([Task.CompletedTask, Task.CompletedTask], task => false);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task anyPairTrue() {
        bool any = await Task2.any(Task.CompletedTask, new TaskCompletionSource().Task, task => true);
        any.Should().BeTrue();
    }

    [Fact]
    public async Task anyPairFalse() {
        bool any = await Task2.any(Task.CompletedTask, Task.CompletedTask, task => false);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task anyVarargsTrue() {
        bool any = await Task2.any(task => true, default, Task.CompletedTask, new TaskCompletionSource().Task);
        any.Should().BeTrue();
    }

    [Fact]
    public async Task anyVarargsFalse() {
        bool any = await Task2.any(task => false, default, Task.CompletedTask, Task.CompletedTask);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task firstOrDefaultEnumerableNotNull() {
        string? actual = await Task2.firstOrDefault([Task.FromResult("a"), Task.FromResult("b"), new TaskCompletionSource<string>().Task], task => task.Result == "b");
        actual.Should().Be("b");
    }

    [Fact]
    public async Task firstOrDefaultEnumerableNotDefault() {
        int? actual = await Task2.firstOrDefault([Task.FromResult(1), Task.FromResult(2), new TaskCompletionSource<int>().Task], task => task.Result == 2);
        actual.Should().Be(2);
    }

    [Fact]
    public async Task firstOrDefaultEnumerableNull() {
        string? actual = await Task2.firstOrDefault(Task.FromResult("a"), Task.FromResult("b"), task => task.Result == "c");
        actual.Should().BeNull();
    }

    [Fact]
    public async Task firstOrDefaultEnumerableDefault() {
        int actual = await Task2.firstOrDefault(task => task.Result == 3, null, Task.FromResult(1), Task.FromResult(2));
        actual.Should().Be(default);
    }

}*/

