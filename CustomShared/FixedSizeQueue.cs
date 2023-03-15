using System;
using System.Collections.Concurrent;

namespace CustomShared;

public class FixedSizedQueue<T> : ConcurrentQueue<T>
{
    private readonly object syncObject = new();

    public int Size { get; }

    public FixedSizedQueue(
        int size)
    {
        if (size == 0)
            throw new ArgumentException();

        Size = size;
    }

    public new void Enqueue(
        T obj)
    {
        base.Enqueue(obj);
        lock (syncObject)
        {
            if (Size < 0)
                return;

            while (Count > Size)
                TryDequeue(out _);
        }
    }
}
