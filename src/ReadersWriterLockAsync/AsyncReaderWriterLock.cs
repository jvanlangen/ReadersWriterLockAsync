// VanLangen.biz licenses this file to you under the MIT license.
// Source: https://github.com/jvanlangen/ReadersWriterLockAsync
// Nuget: https://www.nuget.org/packages/VanLangen.Locking.ReadersWriterLockAsync/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VanLangen.Locking
{
    public sealed class AsyncReadersWriterLock
    {
        private static readonly SynchronizationContext _defaultContext = new SynchronizationContext();
        private readonly List<ExecuteInfo> _readersWritersQueue = new List<ExecuteInfo>();
        private int _activeReaders;
        private bool _writerActive;

        /// <summary>
        /// Execute async code within a reader lock. 
        /// This overload is used when you want to execute async code in a reader lock without returning a value.
        /// </summary>
        /// <param name="asyncAction">The action to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask UseReaderAsync(Func<ValueTask> asyncAction) =>
            ExecuteWithinLockAsync(false, asyncAction);

        /// <summary>
        /// Execute async code within a reader lock. 
        /// This overload is used when you want to execute async code in a reader lock which returns a value.
        /// </summary>
        /// <param name="asyncFunc">The function to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask<T> UseReaderAsync<T>(Func<ValueTask<T>> asyncFunc) =>
            ExecuteWithinLockAsync(false, asyncFunc);

        /// <summary>
        /// Execute code within a reader lock. 
        /// This overload is used when you want to execute non-async code in a reader lock which returns a value.
        /// </summary>
        /// <param name="func">The function to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask<T> UseReaderAsync<T>(Func<T> func) =>
            ExecuteWithinLockAsync(false, () => ValueTask.FromResult<T>(func()));


        /// <summary>
        /// Execute code within a reader lock. 
        /// This overload is used when you want to execute non-async code in a reader lock.
        /// </summary>
        /// <param name="func">The action to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask UseReaderAsync(Action action) =>
             ExecuteWithinLockAsync(false, () =>
             {
                 action();
                 return ValueTask.CompletedTask;
             });


        /// <summary>
        /// Execute async code within a writer lock. 
        /// This overload is used when you want to execute async code in a writer lock without returning a value.
        /// </summary>
        /// <param name="asyncAction">The action to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask UseWriterAsync<T>(Func<ValueTask> asyncAction) =>
            ExecuteWithinLockAsync(true, asyncAction);

        /// <summary>
        /// Execute async code within a writer lock. 
        /// This overload is used when you want to execute async code in a writer lock which returns a value.
        /// </summary>
        /// <param name="asyncFunc">The function to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask<T> UseWriterAsync<T>(Func<ValueTask<T>> asyncFunc) =>
            ExecuteWithinLockAsync(true, asyncFunc);

        /// <summary>
        /// Execute code within a writer lock. 
        /// This overload is used when you want to execute non-async code in a writer lock which returns a value.
        /// </summary>
        /// <param name="func">The function to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask<T> UseWriterAsync<T>(Func<T> func) =>
            ExecuteWithinLockAsync(true, () => ValueTask.FromResult<T>(func()));

        /// <summary>
        /// Execute code within a writer lock. 
        /// This overload is used when you want to execute non-async code in a writer lock.
        /// </summary>
        /// <param name="action">The action to be executed</param>
        /// <returns>A ValueTask that should be checked if it is completed</returns>
        public ValueTask UseWriterAsync(Action action) =>
            ExecuteWithinLockAsync(true, () =>
            {
                action();
                return ValueTask.CompletedTask;
            });


        private TaskCompletionSource<object> CreateTaskCompletionSourceWhenQueued(bool isWriterLock)
        {
            // If there is a write active || if there are readers active and the requested is a writer ||
            // anything is in the queue => Queue it
            //
            // Which means:
            // - Queue readers when a writer is active or when anything is queued.
            // - Queue writers when another writer is active, any readerlocks are active
            //   or when anything is queued.
            //
            if ((isWriterLock && (_activeReaders > 0)) || _writerActive || _readersWritersQueue.Count > 0)
            {
                // queue it and await it some below until it is triggered by a current held lock routine
                var tcs = new TaskCompletionSource<object>();
                // add to the queue and preserve the synchronization context, if non use the default (threadpool)
                _readersWritersQueue.Add(new ExecuteInfo(isWriterLock, tcs, SynchronizationContext.Current ?? _defaultContext));

                return tcs;
            }
            else
                return default;
        }

        private async ValueTask<T> ExecuteWithinLockAsync<T>(bool isWriterLock, Func<ValueTask<T>> asyncFunc)
        {
            TaskCompletionSource<object> tcs = default;

            // lets check if any lock is held (this lock may run parallel)
            // if, for example, this is a writerlock and there is already a readerlock active,
            // this asyncAction will be queued.
            lock (_readersWritersQueue)
            {
                // check if the function can be executed directly.
                tcs = CreateTaskCompletionSourceWhenQueued(isWriterLock);

                // we can execute directly (tcs is not assigned), so take the lock (either reader/writer)
                if (tcs == default)
                    if (isWriterLock)
                        _writerActive = true;
                    else
                        _activeReaders++;
            }

            // if the tcs is assigned, means that it was queued, wait here (outside the lock)
            if (tcs != default)
                await tcs.Task;

            try
            {
                // execute the function
                var result = asyncFunc();

                // if it isn't completed (by using async code) await it here.
                if (!result.IsCompleted)
                    await result;

                return result.Result;
            }
            finally
            {
                // lock the queue again and check if any locks need to be triggered.
                lock (_readersWritersQueue)
                {
                    // we first need to disable this lock, because we were ready with the operation.
                    if (isWriterLock)
                        _writerActive = false;
                    else
                        _activeReaders--;

                    CheckWaitingTasksOnQueue();
                }
            }
        }

        private async ValueTask ExecuteWithinLockAsync(bool isWriterLock, Func<ValueTask> asyncAction)
        {
            TaskCompletionSource<object> tcs = default;

            // lets check if any lock is held (this lock may run parallel)
            // if, for example, this is a writerlock and there is already a readerlock active,
            // this asyncAction will be queued.
            lock (_readersWritersQueue)
            {
                // check if the function can be executed directly.
                tcs = CreateTaskCompletionSourceWhenQueued(isWriterLock);

                // we can execute directly (tcs is not assigned), so take the lock (either reader/writer)
                if (tcs == default)
                    if (isWriterLock)
                        _writerActive = true;
                    else
                        _activeReaders++;
            }

            // if the tcs is assigned, means that it was queued, wait here
            if (tcs != default)
                await tcs.Task;

            try
            {
                // execute the function
                var result = asyncAction();

                // if it isn't completed (by using async code) await it here.
                if (!result.IsCompleted)
                    await result;

            }
            finally
            {
                // lock the queue again and check if any locks need to be triggered.
                lock (_readersWritersQueue)
                {
                    // we first need to disable this lock, because we were ready with the operation.
                    if (isWriterLock)
                        _writerActive = false;
                    else
                        _activeReaders--;

                    CheckWaitingTasksOnQueue();
                }
            }
        }

        private void CheckWaitingTasksOnQueue()
        {
            // check the queue for waiting locks
            while (true)
            {
                // get the first item
                var item = _readersWritersQueue.FirstOrDefault();

                if (item == default)
                    break; // The queue is empty

                // is it a writerlock?
                if (item.IsWriterLock)
                {
                    // if there are active readers, we're not allowed to run (yet) because all readers
                    // must be finished. Stop checking the queue to preserve the order.
                    if (_activeReaders > 0)
                        break;

                    // we're allowed to run this writerlock, so remove from queue. Take the writerlock
                    // and continue the waiting writerlock task
                    _readersWritersQueue.RemoveAt(0);
                    _writerActive = true;

                    // Post the SetResult on the preserved synchronization context
                    item.Context.Post(item.TCS.SetResult, null);

                    // only one writer can be active, so stop checking the queue
                    break;
                }
                // item is a readerlock, the reader is allowed to run, inc active readers and continue
                // the waiting readerlock task. We can assume there is not writer active, 
                // because a reader or writer cannot finish when another writer is active.
                _readersWritersQueue.RemoveAt(0);
                _activeReaders++;

                // Post the SetResult on the preserved synchronization context
                item.Context.Post(item.TCS.SetResult, null);
            }
        }
    }
}