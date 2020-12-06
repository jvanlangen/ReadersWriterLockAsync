# ReadersWriterLockAsync
ReadersWriterLockAsync solves the problem for using multiple readers and a single writer lock using async code.

**This version is not released yet i'm still working on some tests**

# Examples:

*The Write function adds some stopwatch time to the output*

## Async example:

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
 
## Non async example:

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

## Some readers and writers

Let's add two readers and then two writers followed by a reader. The expected behavior should be:
1) reader A and B should run parallel
2) writer C
3) writer D
4) reader E

```csharp
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
{
    if (!valueTask.IsCompleted)
        await valueTask;
}
```
Output:
```
      23,056 ms | Reader A start
      53,059 ms | Reader B start
   1.073,641 ms | Reader B end
   1.073,820 ms | Reader A end
   1.078,123 ms | Writer C start
   2.086,785 ms | Writer C end
   2.087,468 ms | Writer D start
   3.096,612 ms | Writer D end
   3.097,310 ms | Reader E start
   4.102,154 ms | Reader E end
```
