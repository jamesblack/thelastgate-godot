using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace TheLastGate.Planner;

// Runs the search for the newest input on its own: For answers once the search has finished, and an answer for an
// older input is dropped, so only an answer for the input asked for is ever given.
public sealed class FocusJob<TInput, TResult>(
    Func<TInput, CancellationToken, TResult> search,
    Action<Action> startSearch)
    where TInput : IEquatable<TInput>
    where TResult : class
{
    private readonly object gate = new();
    private (TInput Input, TResult? Result, ExceptionDispatchInfo? Failure)? answer;
    private bool finished;
    private (TInput Input, CancellationTokenSource Cancel)? running;

    public TResult? For(TInput input)
    {
        CancellationTokenSource cancel;
        lock (gate)
        {
            if (answer is { } known && known.Input.Equals(input))
            {
                if (known.Failure is { } failure)
                {
                    answer = null;
                    failure.Throw();
                }

                return known.Result;
            }

            if (running is { } current && current.Input.Equals(input))
            {
                return null;
            }

            running?.Cancel.Cancel();
            cancel = new CancellationTokenSource();
            running = (input, cancel);
        }

        startSearch(() => Run(input, cancel));
        lock (gate)
        {
            return answer is { } known && known.Input.Equals(input) && known.Failure == null ? known.Result : null;
        }
    }

    public bool TakeFinished()
    {
        lock (gate)
        {
            var wasFinished = finished;
            finished = false;
            return wasFinished;
        }
    }

    private void Run(TInput input, CancellationTokenSource cancel)
    {
        TResult? result = null;
        ExceptionDispatchInfo? failure = null;
        try
        {
            result = search(input, cancel.Token);
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
        }

        lock (gate)
        {
            if (running is not { } current || current.Cancel != cancel)
            {
                return;
            }

            running = null;
            answer = (input, result, failure);
            finished = true;
        }
    }
}
