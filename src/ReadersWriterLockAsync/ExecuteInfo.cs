using System.Threading.Tasks;

namespace VanLangen.Locking
{
    /// <summary>
    /// Struct to tie the lock type and the TaskCompletionSource, to trigger the awaited lock task
    /// </summary>
    internal struct ExecuteInfo
    {
        public readonly bool IsWriterLock;
        public readonly TaskCompletionSource<object> TCS;

        public ExecuteInfo(bool isWriterLock, TaskCompletionSource<object> tcs)
        {
            IsWriterLock = isWriterLock;
            TCS = tcs;
        }
    }
}