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

       7,013 ms | Before calling UseReaderAsync
      38,953 ms | Reader start
      58,376 ms | After calling UseReaderAsync
      58,500 ms | result.IsCompleted == false, awaiting
   1.078,985 ms | Reader end
   1.079,209 ms | awaiting ready
 
 
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

      19,983 ms | Before calling UseReaderAsync
      46,000 ms | Reader start
      46,100 ms | Reader end
      46,625 ms | After calling UseReaderAsync
      46,666 ms | result.IsCompleted == true
