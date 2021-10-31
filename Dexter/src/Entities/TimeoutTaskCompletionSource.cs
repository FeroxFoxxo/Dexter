using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fergun.Interactive
{
    /// <summary>
    /// Represents a <see cref="TaskCompletionSource{TResult}"/> with a timeout timer which can be reset.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value associated with this <see cref="TimeoutTaskCompletionSource{TResult}"/>.</typeparam>
    internal sealed class TimeoutTaskCompletionSource<TResult>
    {
        /// <summary>
        /// Gets the delay before the timeout.
        /// </summary>
        public TimeSpan Delay { get; }

        /// <summary>
        /// Gets whether this delay can be reset.
        /// </summary>
        public bool CanReset { get; }

        /// <summary>
        /// Gets the timeout result.
        /// </summary>
        public TResult? TimeoutResult => _timeoutAction is null ? _timeoutResult : _timeoutAction();

        /// <summary>
        /// Gets the cancel result.
        /// </summary>
        public TResult? CancelResult => _cancelAction is null ? _cancelResult : _cancelAction();

        /// <summary>
        /// Gets the <see cref="Task{TResult}"/> created by this <see cref="TimeoutTaskCompletionSource{TResult}"/>.
        /// </summary>
        public Task<TResult> Task => _taskSource.Task;

        private bool _disposed;
        private readonly Timer _timer;
        private readonly TaskCompletionSource<TResult> _taskSource;
        private readonly TResult? _timeoutResult;
        private readonly TResult? _cancelResult;
        private readonly Func<TResult>? _timeoutAction;
        private readonly Func<TResult>? _cancelAction;
        private CancellationTokenRegistration _tokenRegistration; // Do not make readonly

        private TimeoutTaskCompletionSource(TimeSpan delay, bool canReset = true, CancellationToken cancellationToken = default)
        {
            Delay = delay;
            CanReset = canReset;
            _taskSource = new TaskCompletionSource<TResult>();
            _timer = new Timer(OnTimerFired, null, delay, Timeout.InfiniteTimeSpan);
            _tokenRegistration = cancellationToken.Register(() => TryCancel());
        }

        public TimeoutTaskCompletionSource(TimeSpan delay, bool canReset = true, TResult? timeoutResult = default,
            TResult? cancelResult = default, CancellationToken cancellationToken = default)
            : this(delay, canReset, cancellationToken)
        {
            _timeoutResult = timeoutResult;
            _cancelResult = cancelResult;
        }

        public TimeoutTaskCompletionSource(TimeSpan delay, bool canReset = true, Func<TResult>? timeoutAction = default,
            Func<TResult>? cancelAction = default, CancellationToken cancellationToken = default)
            : this(delay, canReset, cancellationToken)
        {
            _timeoutAction = timeoutAction;
            _cancelAction = cancelAction;
        }

        private void OnTimerFired(object state)
        {
            _disposed = true;
            _timer.Dispose();
            TrySetResult(TimeoutResult);
            _tokenRegistration.Dispose();
        }

        /// <summary>
        /// Attempts to reset the internal <see cref="Timer"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryReset() => !_disposed && CanReset && _timer.Change(Delay, Timeout.InfiniteTimeSpan);

        /// <summary>
        /// Attempts to cancel the underlying <see cref="TaskCompletionSource{TResult}"/> using <see cref="CancelResult"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryCancel() => !_disposed && TrySetResult(CancelResult);

        /// <summary>
        /// Attempts to set the result of the underlying <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        /// <param name="result">The result to set.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        public bool TrySetResult(TResult? result) => _taskSource.TrySetResult(result!);

        /// <summary>
        /// Attempts to dispose the internal <see cref="Timer"/> and cancel the underlying <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        public bool TryDispose()
        {
            if (_disposed)
            {
                return false;
            }

            _timer.Dispose();
            TryCancel();
            _tokenRegistration.Dispose();
            _disposed = true;

            return true;
        }
    }
}