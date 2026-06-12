#if !NET5_0_OR_GREATER

#pragma warning disable CS1591

namespace System.Threading.Tasks;

#pragma warning disable CA1034

public static class ValueTaskExtensions
{
    extension(ValueTask)
    {
        public static ValueTask<TResult> FromResult<TResult>(TResult result)
        {
            return new ValueTask<TResult>(result);
        }

        public static ValueTask FromException(Exception exception)
        {
            return new ValueTask(Task.FromException(exception));
        }

        public static ValueTask<TResult> FromException<TResult>(Exception exception)
        {
            return new ValueTask<TResult>(Task.FromException<TResult>(exception));
        }

        public static ValueTask FromCanceled(CancellationToken cancellationToken)
        {
            return new ValueTask(Task.FromCanceled(cancellationToken));
        }

        public static ValueTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
        {
            return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
        }
    }
}

#endif
