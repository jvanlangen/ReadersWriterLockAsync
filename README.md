# ReadersWriterLockAsync
ReadersWriterLockAsync solves the problem for using multiple readers and a single writer lock using async code.

**This version is not released yet i'm still working on some tests**

# Examples:

*The Write function adds some stopwatch time to the output*

**Async example:**

```csharp
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
```
Output:
```
      21,078 ms | Before calling UseReaderAsync
      51,063 ms | Reader start
      70,050 ms | After calling UseReaderAsync
      70,193 ms | result.IsCompleted == false, awaiting
   1.083,426 ms | Reader end
   1.083,705 ms | awaiting ready
```
   
As you can see the "Reader start" is executed directly. Because of the await, the result.IsComplete is not set. We're able to await the result.
 
**Non async example:**

```csharp
Write("Before calling UseReaderAsync");
var result = _readersWriterLock.UseReaderAsync(() =>
{
    Write("Reader start");
    // await Task.Delay(1000);
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
```
Output:
```
      19,983 ms | Before calling UseReaderAsync
      46,000 ms | Reader start
      46,100 ms | Reader end
      46,625 ms | After calling UseReaderAsync
      46,666 ms | result.IsCompleted == true
```

Here no await is used and the method completes directly. No await is needed, which speeds-up the system.

**Some readers and writers**

Let's add two readers and then two writers. The expected behavior shoudl be:
- two readers parallel
- writer
- writer

```csharp
           // Setup a list and add some readers and writers.
            var allValueTasks = new List<ValueTask>
            {
                // the first reader will run directly
                _readersWriterLock.UseReaderAsync(async () =>
                {
                    Write("Reader 1 start");
                    await Task.Delay(1000);
                    Write("Reader 1 end");
                }),
                // the second reader will also run directly
                _readersWriterLock.UseReaderAsync(async () =>
                {
                    Write("Reader 2 start");
                    await Task.Delay(1000);
                    Write("Reader 2 end");
                }),
                // because of two readers, this writer has to be queued
                _readersWriterLock.UseWriterAsync(async () =>
                {
                    Write("Writer 1 start");
                    await Task.Delay(1000);
                    Write("Writer 1 end");
                }),
                // because of two readers and a writer queued, this writer has to be queued also
                _readersWriterLock.UseWriterAsync(async () =>
                {
                    Write("Writer 2 start");
                    await Task.Delay(1000);
                    Write("Writer 2 end");
                })
            };

            foreach (var valueTask in allValueTasks)
            {
                if (!valueTask.IsCompleted)
                    await valueTask;
            }
```
