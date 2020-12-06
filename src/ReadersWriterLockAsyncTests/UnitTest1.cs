using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using VanLangen.Locking;
using Xunit;

namespace ReadersWriterLockAsyncTests
{
    public class UnitTest1
    {
        [Fact]
        public async void ExecuteSync()
        {
            var rwl = new AsyncReadersWriterLock();

            var results = new List<int>();
            
            var reader1 = rwl.UseReaderAsync(() =>
            {
                results.Add(1);
                return new ValueTask();
            });

            var writer2 = rwl.UseWriterAsync(() =>
            {
                results.Add(2);
                return new ValueTask();
            });

            var reader3 = rwl.UseReaderAsync(() =>
            {
                results.Add(3);
                return new ValueTask();
            });

            Assert.True(results[0] == 1, "At index 0 should be value 1");
            Assert.True(results[1] == 2, "At index 1 should be value 2");
            Assert.True(results[2] == 3, "At index 2 should be value 3");
        }

        [Fact]
        public async void ExecuteAsync()
        {
            var rwl = new AsyncReadersWriterLock();

            var results = new List<int>();

            var reader1 = rwl.UseReaderAsync(async () =>
            {
                await Task.Delay(10);
                results.Add(1);
            });

            if (!reader1.IsCompleted)
                await reader1;

            var writer2 = rwl.UseWriterAsync(async () =>
            {
                await Task.Delay(10);
                results.Add(2);
            });

            if (!writer2.IsCompleted)
                await writer2;

            var reader3 = rwl.UseReaderAsync(async () =>
            {
                await Task.Delay(10);
                results.Add(3);
            });

            if (!reader3.IsCompleted)
                await reader3;

            Assert.True(results[0] == 1, "At index 0 should be value 1");
            Assert.True(results[1] == 2, "At index 1 should be value 2");
            Assert.True(results[2] == 3, "At index 2 should be value 3");
        }
    }
}
