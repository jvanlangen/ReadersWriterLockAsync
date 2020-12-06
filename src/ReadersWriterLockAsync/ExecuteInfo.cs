// VanLangen.biz licenses this file to you under the MIT license.
// Source: https://github.com/jvanlangen/ReadersWriterLockAsync
// Nuget: https://www.nuget.org/packages/VanLangen.Locking.ReadersWriterLockAsync/
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