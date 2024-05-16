namespace GitHooks.Tasks;

public static class Task2 {

    public static async Task<bool> any(IEnumerable<Task> childTasks, Predicate<Task> predicate, CancellationTokenSource? cts = default) {
        TaskCompletionSource predicatePassed = new();

        Task allChildrenDone = Task.WhenAll(childTasks.Select(childTask => childTask.ContinueWith(c => {
            if (predicate(c)) {
                predicatePassed.TrySetResult();
                cts?.Cancel();
            }
        }, TaskContinuationOptions.OnlyOnRanToCompletion)));

        await Task.WhenAny(predicatePassed.Task, allChildrenDone);
        return predicatePassed.Task.IsCompletedSuccessfully;
    }

    public static Task<bool> any(Task childTask1, Task childTask2, Predicate<Task> predicate, CancellationTokenSource? cts = default) {
        return any([childTask1, childTask2], predicate, cts);
    }

    public static Task<bool> any(Predicate<Task> predicate, CancellationTokenSource? cts = default, params Task[] childTasks) {
        return any(childTasks, predicate, cts);
    }

    public static async Task<T?> firstOrDefault<T>(IEnumerable<Task<T>> childTasks, Predicate<Task<T>> predicate, CancellationTokenSource? cts = default) {
        TaskCompletionSource<T> predicatePassed = new();

        Task<T[]> allChildrenDone = Task.WhenAll(childTasks.Select(childTask => childTask.ContinueWith(c => {
            if (predicate(c)) {
                predicatePassed.TrySetResult(c.Result);
                cts?.Cancel();
            }

            return c.Result;
        }, TaskContinuationOptions.OnlyOnRanToCompletion)));

        await Task.WhenAny(predicatePassed.Task, allChildrenDone);

        return predicatePassed.Task is { IsCompletedSuccessfully: true, Result: { } result } ? result : default;
    }

    public static Task<T?> firstOrDefault<T>(Task<T> childTask1, Task<T> childTask2, Predicate<Task<T>> predicate, CancellationTokenSource? cts = default) {
        return firstOrDefault([childTask1, childTask2], predicate, cts);
    }

    public static Task<T?> firstOrDefault<T>(Predicate<Task<T>> predicate, CancellationTokenSource? cts = default, params Task<T>[] childTasks) {
        return firstOrDefault(childTasks, predicate, cts);
    }

}