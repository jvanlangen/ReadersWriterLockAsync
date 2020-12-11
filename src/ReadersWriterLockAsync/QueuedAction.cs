// VanLangen.biz licenses this file to you under the MIT license.
// Source: https://github.com/jvanlangen/ReadersWriterLockAsync
// Nuget: https://www.nuget.org/packages/VanLangen.Locking.ReadersWriterLockAsync/
using System.Threading;
using System.Threading.Tasks;

namespace VanLangen.Locking
{
    /// <summary>
    /// Struct to tie the lock type and the TaskCompletionSource, to trigger the awaited lock task
    /// </summary>
    internal class QueuedAction
    {
        public bool IsWriterLock { get; }
        public TaskCompletionSource<object> TCS { get; }
        public SynchronizationContext Context { get; }

        public QueuedAction(bool isWriterLock, TaskCompletionSource<object> tcs, SynchronizationContext context)
        {
            IsWriterLock = isWriterLock;
            TCS = tcs;
            Context = context;
        }
    }
}