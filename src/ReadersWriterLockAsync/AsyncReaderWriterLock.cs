using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VanLangen.Locking
{
    public class AsyncReadersWriterLock
    {
        /// <summary>
        /// Struct to tie the lock type and the TaskCompletionSource, to trigger the awaited lock task
        /// </summary>
        private struct ExecuteInfo
        {
            public readonly bool IsWriterLock;
            public readonly TaskCompletionSource<object> TCS;

            public ExecuteInfo(bool isWriterLock, TaskCompletionSource<object> tcs)
            {
                IsWriterLock = isWriterLock;
                TCS = tcs;
            }
        }

        private readonly List<ExecuteInfo> _readersWritersQueue = new List<ExecuteInfo>();
        private int _activeReaders;
        private bool _writerActive;

        /// <summary>
        /// Execute this code within a reader lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns>Task which can be awaited</returns>
        public ValueTask InReaderLockAsync(Func<ValueTask> asyncAction) =>
            ExecuteWithinLockAsync(false, asyncAction);

        /// <summary>
        /// Execute this code within a writer lock
        /// </summary>
        /// <param name="asyncAction"></param>
        /// <returns></returns>
        public ValueTask InWriterLockAsync(Func<ValueTask> asyncAction) =>
            ExecuteWithinLockAsync(true, asyncAction);


        private async ValueTask ExecuteWithinLockAsync(bool isWriterLock, Func<ValueTask> asyncAction)
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
                    // add to the queue
                    _readersWritersQueue.Add(new ExecuteInfo(isWriterLock, tcs));
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

            // execute the function
            var result = asyncAction();

            // if it isn't completed (by using async code) await it here.
            if (!result.IsCompleted)
                await result;

            // lock the queue again and check if any locks need to be triggered.
            lock (_readersWritersQueue)
            {
                // we first need to disable this lock, because we were ready with the operation.
                if (isWriterLock)
                    _writerActive = false;
                else
                    _activeReaders--;

                // this lock is ready, now check the queue for waiting locks
                while (_readersWritersQueue.Count > 0)
                {
                    // get the first item
                    var item = _readersWritersQueue.First();

                    // is it a writerlock?
                    if (item.IsWriterLock)
                    {
                        // if there are active readers, we're not allowed to run (yet) because all readers
                        // must be finished. Stop checking the queue
                        if (_activeReaders > 0)
                            break;

                        // we're allowed to run this writerlock, so remove from queue. Take the writerlock
                        // and continue the waiting writerlock task
                        _readersWritersQueue.RemoveAt(0);
                        _writerActive = true;
                        item.TCS.SetResult(null);
                        // only one writer can be active, so stop checking the queue
                        break;
                    }
                    // item is a readerlock, the reader is allowed to run, inc active readers and continue
                    // the waiting readerlock task. We can assume there is not writer active, 
                    // because a reader or writer cannot finish when another writer is active.
                    _readersWritersQueue.RemoveAt(0);
                    _activeReaders++;
                    item.TCS.SetResult(null);
                }
            }
        }
    }
}