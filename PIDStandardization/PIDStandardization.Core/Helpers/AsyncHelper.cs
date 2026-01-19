using Serilog;

namespace PIDStandardization.Core.Helpers
{
    /// <summary>
    /// Helper class for handling async operations safely, especially for event handlers
    /// that cannot return Task (async void required)
    /// </summary>
    public static class AsyncHelper
    {
        /// <summary>
        /// Safely executes an async operation, catching and logging any exceptions.
        /// Use this wrapper for async event handlers that must be async void.
        /// </summary>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="onError">Optional callback when an error occurs</param>
        public static async void SafeFireAndForget(
            Func<Task> operation,
            string operationName,
            Action<Exception>? onError = null)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in async operation '{Operation}'",
                    correlationId, operationName);

                onError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Safely executes an async operation with a result, catching and logging any exceptions.
        /// </summary>
        /// <typeparam name="T">The result type</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="onError">Optional callback when an error occurs</param>
        /// <returns>The result or default value on error</returns>
        public static async Task<T?> SafeExecuteAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            T? defaultValue = default,
            Action<Exception>? onError = null)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in async operation '{Operation}'",
                    correlationId, operationName);

                onError?.Invoke(ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Wraps an async void event handler with proper exception handling
        /// </summary>
        /// <param name="handler">The async event handler logic</param>
        /// <param name="handlerName">Name of the handler for logging</param>
        /// <param name="onError">Optional callback when an error occurs</param>
        /// <returns>An event handler that safely executes the async logic</returns>
        public static EventHandler WrapAsyncEventHandler(
            Func<object?, EventArgs, Task> handler,
            string handlerName,
            Action<Exception>? onError = null)
        {
            return (sender, args) => SafeFireAndForget(
                () => handler(sender, args),
                handlerName,
                onError);
        }

        /// <summary>
        /// Wraps an async void event handler with proper exception handling (for RoutedEventHandler)
        /// </summary>
        public static Action<object, object> WrapAsyncRoutedEventHandler(
            Func<object, object, Task> handler,
            string handlerName,
            Action<Exception>? onError = null)
        {
            return (sender, args) => SafeFireAndForget(
                () => handler(sender, args),
                handlerName,
                onError);
        }

        /// <summary>
        /// Runs an async Task synchronously on the current thread (use sparingly)
        /// </summary>
        public static void RunSync(Func<Task> operation)
        {
            var oldContext = SynchronizationContext.Current;
            var singleThreadedContext = new SingleThreadedSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(singleThreadedContext);

            try
            {
                var task = operation();
                task.ContinueWith(t =>
                {
                    singleThreadedContext.Complete();
                }, TaskScheduler.Default);

                singleThreadedContext.RunOnCurrentThread();
                task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        /// <summary>
        /// Runs an async Task synchronously and returns the result (use sparingly)
        /// </summary>
        public static T RunSync<T>(Func<Task<T>> operation)
        {
            var oldContext = SynchronizationContext.Current;
            var singleThreadedContext = new SingleThreadedSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(singleThreadedContext);

            try
            {
                var task = operation();
                task.ContinueWith(t =>
                {
                    singleThreadedContext.Complete();
                }, TaskScheduler.Default);

                singleThreadedContext.RunOnCurrentThread();
                return task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        /// <summary>
        /// Single-threaded synchronization context for blocking async operations
        /// </summary>
        private sealed class SingleThreadedSynchronizationContext : SynchronizationContext
        {
            private readonly BlockingCollection<(SendOrPostCallback, object?)> _queue =
                new BlockingCollection<(SendOrPostCallback, object?)>();
            private bool _isComplete;

            public override void Post(SendOrPostCallback d, object? state)
            {
                if (!_isComplete)
                {
                    _queue.Add((d, state));
                }
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotSupportedException("Synchronous send is not supported");
            }

            public void RunOnCurrentThread()
            {
                while (!_isComplete || _queue.Count > 0)
                {
                    if (_queue.TryTake(out var workItem, 100))
                    {
                        workItem.Item1(workItem.Item2);
                    }
                }
            }

            public void Complete()
            {
                _isComplete = true;
            }
        }
    }
}
