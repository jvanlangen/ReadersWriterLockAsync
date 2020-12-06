# ReadersWriterLockAsync
ReadersWriterLockAsync solves the problem for using multiple readers and a single writer lock using async code.

**This version is not released yet i'm still working on some tests**

Examples:

```csharp
            var result = _readersWriterLock.UseReaderAsync(async () =>
            {
                Write("Reader 1 start");
                await Task.Delay(1000);
                Write("Reader 1 end");
            });
```
