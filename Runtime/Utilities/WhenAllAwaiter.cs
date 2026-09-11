using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;


namespace UniMod.Utilities {
    /// <summary>
    /// Utility class to await multiple task with proper exception throwing.
    /// </summary>
    sealed class WhenAllAwaiter {
        public bool IsAwaiting { get; set; }

        readonly List<UniTask> _wrappedTasks = new();
        readonly List<Exception> _exceptions = new();

        public async UniTask WaitAndThrowAll(IEnumerable<UniTask> tasks) {
            if (IsAwaiting)
                throw new Exception("Already awaiting a previous call...");

            IsAwaiting = true;

            // create the wrapped tasks that will catch the exceptions
            IEnumerator<UniTask> enumerator = tasks.GetEnumerator();
            while (enumerator.MoveNext())
                _wrappedTasks.Add(GetWrappedTask(enumerator.Current));
            enumerator.Dispose();

            await UniTask.WhenAll(_wrappedTasks);
            _wrappedTasks.Clear();

            // TODO: consider a lock
            switch (_exceptions.Count) {
                case 0:
                    IsAwaiting = false;
                    return;
                case 1:
                    Exception exception = _exceptions[0];
                    _exceptions.Clear();
                    IsAwaiting = false;
                    throw exception;
            }

            AggregateException aggregateException = new(_exceptions);
            _exceptions.Clear();
            IsAwaiting = false;
            throw aggregateException;
        }

        async UniTask GetWrappedTask(UniTask task) {
            try {
                await task;
            } catch (Exception exception) {
                lock (_exceptions) {
                    _exceptions.Add(exception);
                }
            }
        }
    }

    sealed class WhenAllAwaiter<T> {
        public bool IsAwaiting { get; set; }

        readonly List<UniTask<T>> _wrappedTasks = new();
        readonly List<Exception> _exceptions = new();

        public async UniTask<T[]> WhenAll(IEnumerable<UniTask<T>> tasks) {
            if (IsAwaiting)
                throw new Exception("Already awaiting a previous call...");

            IsAwaiting = true;

            IEnumerator<UniTask<T>> enumerator = tasks.GetEnumerator();
            while (enumerator.MoveNext())
                _wrappedTasks.Add(GetWrappedTask(enumerator.Current));
            enumerator.Dispose();

            T[] result = await UniTask.WhenAll(_wrappedTasks);
            _wrappedTasks.Clear();

            switch (_exceptions.Count) {
                case 0:
                    IsAwaiting = false;
                    return result;
                case 1:
                    Exception exception = _exceptions[0];
                    _exceptions.Clear();
                    IsAwaiting = false;
                    throw exception;
            }

            AggregateException aggregateException = new(_exceptions);
            _exceptions.Clear();
            IsAwaiting = false;
            throw aggregateException;
        }

        public async UniTask<(T[] result, Exception exception)> WhenAllWithResult(IEnumerable<UniTask<T>> tasks) {
            if (IsAwaiting)
                throw new Exception("Already awaiting a previous call...");

            IsAwaiting = true;

            IEnumerator<UniTask<T>> enumerator = tasks.GetEnumerator();
            while (enumerator.MoveNext())
                _wrappedTasks.Add(GetWrappedTask(enumerator.Current));
            enumerator.Dispose();

            T[] result = await UniTask.WhenAll(_wrappedTasks);
            _wrappedTasks.Clear();

            switch (_exceptions.Count) {
                case 0:
                    IsAwaiting = false;
                    return (result, null);
                case 1:
                    Exception exception = _exceptions[0];
                    _exceptions.Clear();
                    IsAwaiting = false;
                    return (result, exception);
            }

            AggregateException aggregateException = new(_exceptions);
            _exceptions.Clear();
            IsAwaiting = false;
            return (result, aggregateException);
        }

        async UniTask<T> GetWrappedTask(UniTask<T> task) {
            try {
                return await task;
            } catch (Exception exception) {
                _exceptions.Add(exception);
                return default;
            }
        }
    }
}