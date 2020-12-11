using System;
using System.Threading.Tasks;
using VanLangen.Locking;

namespace TestReadersWriterLockAsync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rwl = new AsyncReadersWriterLock();

            Console.WriteLine("* Example 1");

            // run example 1
            var result1 = Example1(rwl);

            if (!result1.IsCompleted)
                await result1;

            Console.WriteLine("");

            Console.WriteLine("* Example 2");

            // run example 2
            var result2 = Example2(rwl);

            if (!result2.IsCompleted)
                await result2;
        }

        private static async ValueTask Example1(AsyncReadersWriterLock rwl)
        {
            Console.WriteLine("Before calling UseReaderAsync");
            var result = rwl.UseReaderAsync(async () =>
            {
                Console.WriteLine("Reader start");
                await Task.Delay(1000);
                Console.WriteLine("Reader end");
            });
            Console.WriteLine("After calling UseReaderAsync");
            Console.WriteLine("");

            if (!result.IsCompleted)
            {
                Console.WriteLine("result.IsCompleted == false, awaiting");
                await result;
                Console.WriteLine("await finished");
            }
            else
                Console.WriteLine("result.IsCompleted == true, no await is used");
        }

        private static async ValueTask Example2(AsyncReadersWriterLock rwl)
        {
            Console.WriteLine("Before calling UseReaderAsync");
            var result = rwl.UseReaderAsync(() =>
            {
                Console.WriteLine("Reader start");
                // no async is used
                //await Task.Delay(1000);
                Console.WriteLine("Reader end");
            });
            Console.WriteLine("After calling UseReaderAsync");
            Console.WriteLine("");

            if (!result.IsCompleted)
            {
                Console.WriteLine("result.IsCompleted == false, awaiting");
                await result;
                Console.WriteLine("await finished");
            }
            else
                Console.WriteLine("result.IsCompleted == true, no await is used");
        }
    }
}
