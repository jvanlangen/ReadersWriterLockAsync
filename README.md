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
    Write("Reader 1 start");
    await Task.Delay(1000);
    Write("Reader 1 end");
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

       6,142 ms | Before calling UseReaderAsync
      34,053 ms | Reader 1 start
      55,544 ms | After calling UseReaderAsync
      55,690 ms | result.IsCompleted == false, awaiting
   1.075,528 ms | Reader 1 end
   1.075,793 ms | awaiting ready
 
 
**Non async example:**

```csharp
Write("Before calling UseReaderAsync");
var result = _readersWriterLock.UseReaderAsync(() =>
{
    Write("Reader 1 start");
    // await Task.Delay(1000);
    Write("Reader 1 end");
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

      6,143 ms | Before calling UseReaderAsync
     32,849 ms | Reader 1 start
     33,026 ms | Reader 1 end
     33,597 ms | After calling UseReaderAsync
     33,674 ms | result.IsCompleted == true
