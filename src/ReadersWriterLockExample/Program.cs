using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VanLangen.Locking;

namespace ReadersWriterLockExample
{
    class Program
    {
        static Stopwatch _sw =  Stopwatch.StartNew();
        static void Write(string line) =>
             Console.WriteLine($"{_sw.Elapsed.TotalMilliseconds,10:N3} ms | {line}");




        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var readersWriterLock = new AsyncReadersWriterLock();

            var writer = readersWriterLock.InWriterLockAsync(async () =>
            {
                Write("Writer start");
                await Task.Delay(1000);
                Write("Writer end");
            });

            var reader1 = readersWriterLock.InReaderLockAsync(async () =>
            {
                Write("Reader 1 start");
                await Task.Delay(1000);
                Write("Reader 1 end");
            });

            var reader2 = readersWriterLock.InReaderLockAsync(async () =>
            {
                Write("Reader 2 start");
                await Task.Delay(1000);
                Write("Reader 2 end");
            });

            var writer2 = readersWriterLock.InWriterLockAsync(async () =>
            {
                Write("Writer 2 start");
                await Task.Delay(1000);
                Write("Writer 2 end");
            });

            var reader3 = readersWriterLock.InReaderLockAsync(async () =>
            {
                Write("Reader 3 start");
                await Task.Delay(1000);
                Write("Reader 3 end");
            });

            // wait until it is completed
            if(!reader3.IsCompleted)
                await reader3;

            var reader4 = readersWriterLock.InReaderLockAsync(async () =>
            {
                Write("reader 4 start");
                await Task.Delay(1000);
                Write("reader 4 end");
            });

            await Task.Delay(10000);
        }
    }
}
