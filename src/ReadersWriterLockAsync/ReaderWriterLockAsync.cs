using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VanLangen.Locking
{
    public class ReadersWriterLockAsync
    {
        private class ExecuteInfo
        {
            public readonly bool IsWriterLock;
            public readonly TaskCompletionSource<object> TCS;

            public ExecuteInfo(bool isWriterLock, TaskCompletionSource<object> tcs)
            {
                IsWriterLock = isWriterLock;
                TCS = tcs;
            }
        }

        public readonly object SyncRoot = new object();

        private readonly List<ExecuteInfo> _readersWritersQueue = new List<ExecuteInfo>();
        private int _activeReaders;
        private bool _writerActive;

        public ValueTask ExecuteInReaderLock(Func<ValueTask> asyncAction) =>
            ExecuteWithinLock(false, asyncAction);

        public ValueTask ExecuteInWriterLock(Func<ValueTask> asyncAction) =>
            ExecuteWithinLock(true, asyncAction);


        private async ValueTask ExecuteWithinLock(bool isWriterLock, Func<ValueTask> asyncAction)
        {
            TaskCompletionSource<object> tcs = default;

            lock (SyncRoot)
            {
                // if there is a write active 
                // or 
                // if there are readers active and the requested is a writer 
                // or 
                // anything is in the queue
                if (_writerActive || ((_activeReaders > 0) && isWriterLock) || _readersWritersQueue.Count > 0)
                {
                    // queue it and await it some below
                    tcs = new TaskCompletionSource<object>();
                    _readersWritersQueue.Add(new ExecuteInfo(isWriterLock, tcs));
                }
                // no, we don't have to await anything.
                else if (isWriterLock)
                    _writerActive = true;
                else
                    _activeReaders++;
            }

            // if the tcs is assigned, means that it was queued, wait here
            if (tcs != default)
                await tcs.Task;

            var result = asyncAction();

            if (!result.IsCompleted)
                await result;

            lock (SyncRoot)
            {
                if (isWriterLock)
                    _writerActive = false;
                else
                    _activeReaders--;

                while (_readersWritersQueue.Count > 0)
                {
                    var item = _readersWritersQueue.First();

                    if (item.IsWriterLock)
                    {
                        // do not execute writer when readers are active.
                        if (_activeReaders > 0)
                            break;

                        _readersWritersQueue.RemoveAt(0);
                        _writerActive = true;
                        item.TCS.SetResult(null);
                        break;
                    }

                    _readersWritersQueue.RemoveAt(0);
                    _activeReaders++;
                    item.TCS.SetResult(null);
                }
            }
        }
    }
}