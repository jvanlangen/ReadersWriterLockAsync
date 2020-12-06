using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using VanLangen.Locking;

namespace ReadersWriterLockExample
{
    class Program
    {
        static Stopwatch _sw = Stopwatch.StartNew();
        static void Write(string line) =>
             Console.WriteLine($"{_sw.Elapsed.TotalMilliseconds,12:N3} ms | {line}");




        static async Task Main(string[] args)
        {
            await Example1();
            //await Example2();
        }

        // create the instance of the readersWriterLock
        private static readonly AsyncReadersWriterLock _readersWriterLock = new AsyncReadersWriterLock();

        private static async Task Example2()
        {
            Write("Before calling UseReaderAsync");
            var result = _readersWriterLock.UseReaderAsync(async () =>
            {
                Write("Reader start");
                await Task.Delay(1000);
                Write("Reader end");
            });
            Write("After calling UseReaderAsync");

            if (!result.IsCompleted)
            {
                Write("result.IsCompleted == false, awaiting");
                await result;
                Write("awaiting ready");
            }
            else
                Write("result.IsCompleted == true");


            //var result = _readersWriterLock.UseReaderAsync(() =>
            //{
            //    Write("Reader 1 start");
            //    Write("Reader 1 end");
            //});


            //var result2 = _readersWriterLock.UseReaderAsync(() =>
            //{
            //    Write("Reader 2 start/end");
            //});

            //var result3 = _readersWriterLock.UseWriterAsync(async () =>
            //{
            //    Write("Write 1 start");
            //    await Task.Delay(1000);
            //    Write("Writer 1 end");
            //});

            // if it could be locked, it will execute directly. (in this example async code is used, so it will
            // be awaited.


            //if (!result2.IsCompleted)
            //    await result2;

            //if (!result3.IsCompleted)
            //    await result3;
        }

        private static async Task Example1()
        {
            // Initialize an array with some readers and writers.
            var allValueTasks = new[]
            {
                // the first reader will run directly
                _readersWriterLock.UseReaderAsync(async () =>
                {
                    Write("Reader A start");
                    await Task.Delay(1000);
                    Write("Reader A end");
                }),
                // the second reader will also run directly
                _readersWriterLock.UseReaderAsync(async () =>
                {
                    Write("Reader B start");
                    await Task.Delay(1000);
                    Write("Reader B end");
                }),
                // because of two readers, this writer has to be queued
                _readersWriterLock.UseWriterAsync(async () =>
                {
                    Write("Writer C start");
                    await Task.Delay(1000);
                    Write("Writer C end");
                }),
                // because of two readers and a writer queued, this writer has to be queued also
                _readersWriterLock.UseWriterAsync(async () =>
                {
                    Write("Writer D start");
                    await Task.Delay(1000);
                    Write("Writer D end");
                }),
                // Lets add another reader, because some writers are queued, this reader is queued also
                _readersWriterLock.UseReaderAsync(async () =>
                {
                    Write("Reader E start");
                    await Task.Delay(1000);
                    Write("Reader E end");
                }),
            };

            foreach (var valueTask in allValueTasks)
                if (!valueTask.IsCompleted)
                    await valueTask;
        }
    }
}
