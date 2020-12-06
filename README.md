# ReadersWriterLockAsync
ReadersWriterLockAsync solves the problem for using multiple readers and a single writer lock using async code.

**This version is not released yet i'm still working on some tests**

# Examples:

*The Write function adds some stopwatch time to the output*

**Async example:**

```csharp
// Execute code within the reader lock
var result = _readersWriterLock.UseReaderAsync(async () =>
{
    Write("Reader 1 start");
    await Task.Delay(1000);
    Write("Reader 1 end");
});

// if it could be locked, it will execute directly. (in this example async code is used, so it will
// be awaited)
if (!result.IsCompleted)
    await result;
```

**Non async example:**

```csharp
// Execute code within the reader lock
var result = _readersWriterLock.UseReaderAsync(async () =>
{
    Write("Reader 1 start");
    // some other non async code
    Write("Reader 1 end");
});

// if it could be locked, it will execute directly. (in this example no async code is used, so it will
// be completed directly)
if (!result.IsCompleted)
    await result;
```
