﻿// VanLangen.biz licenses this file to you under the MIT license.
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
        private readonly List<ExecuteInfo> _readersWritersQueue = new List<ExecuteInfo>();
        private int _activeReaders;
        private bool _writerActive;
        private static readonly SynchronizationContext _defaultContext = new SynchronizationContext();

        /// <summary>
        /// Execute the async code within a reader lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns>Task which can be awaited</returns>
        public ValueTask<T> UseReaderAsync<T>(Func<ValueTask<T>> asyncAction) =>
            ExecuteWithinLockAsync(false, asyncAction);

        /// <summary>
        /// Execute the code within a reader lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns>Task which can be awaited</returns>
        public ValueTask<T> UseReaderAsync<T>(Func<T> asyncAction) =>
            ExecuteWithinLockAsync(false, () => ValueTask.FromResult<T>(asyncAction()));


        /// <summary>
        /// Execute the code within a reader lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns>Task which can be awaited</returns>
        public async ValueTask UseReaderAsync(Action asyncAction)
        {
            var result = ExecuteWithinLockAsync<object>(false, () =>
             {
                 asyncAction();
                 return new ValueTask<object>();
             });

            if (!result.IsCompleted)
                await result;
        }


        /// <summary>
        /// Execute the async code within a writer lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns></returns>
        public ValueTask<T> UseWriterAsync<T>(Func<ValueTask<T>> asyncAction) =>
            ExecuteWithinLockAsync(true, asyncAction);

        /// <summary>
        /// Execute this code within a writer lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns></returns>
        public ValueTask<T> UseWriterAsync<T>(Func<T> asyncAction) =>
            ExecuteWithinLockAsync(true, () => ValueTask.FromResult<T>(asyncAction()));

        /// <summary>
        /// Execute this code within a writer lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns></returns>
        public async ValueTask UseWriterAsync(Action asyncAction)
        {
            var result = ExecuteWithinLockAsync<object>(true, () =>
            {
                asyncAction();
                return new ValueTask<object>();
            });

            if (!result.IsCompleted)
                await result;
        }

        private async ValueTask<T> ExecuteWithinLockAsync<T>(bool isWriterLock, Func<ValueTask<T>> asyncAction)
        {
            TaskCompletionSource<object> tcs = default;

            // lets check if any lock is held (this lock may run parallel)
            // if, for example, this is a writerlock and there is already a readerlock active,
            // this asyncAction will be queued.
            lock (_readersWritersQueue)
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
                    tcs = new TaskCompletionSource<object>();
                    // add to the queue and preserve the synchronization context, if non use the default (threadpool)
                    _readersWritersQueue.Add(new ExecuteInfo(isWriterLock, tcs, SynchronizationContext.Current ?? _defaultContext));
                }
                // we can execute directly, so take the lock (either reader/writer)
                else if (isWriterLock)
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

                    ContinueWaitingTasks();
                }
            }
        }

        private void ContinueWaitingTasks()
        {
            // this lock is ready, now check the queue for waiting locks
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