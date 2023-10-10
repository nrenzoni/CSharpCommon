using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomShared;

public class TaskQueueProcessor<TEntry>
    : IDisposable
{
    private readonly BlockingCollection<TEntry> _queue = new();

    private readonly Task _backgroundPoller;

    private readonly uint _maxEntriesPerProcess;
    private readonly bool _asynchronousSupport;

    public delegate void ProcessEntriesDelegate(
        IEnumerable<TEntry> poppedEntries);

    private readonly ProcessEntriesDelegate _processEntries;

    private readonly AutoResetEvent _shouldFlushARE = new(false);
    private readonly AutoResetEvent _flushedARE = new(false);
    private readonly ManualResetEvent _flushInProgressEvent = new(true);

    private DateTime _lastSave;
    private readonly TimeSpan? _maxSaveDuration;

    private bool _alreadyFinished;

    public TaskQueueProcessor(
        ProcessEntriesDelegate processEntries,
        uint maxEntriesPerProcess,
        bool asynchronousSupport,
        uint? maxSaveDurationSeconds = null)
    {
        _processEntries = processEntries;
        _maxEntriesPerProcess = maxEntriesPerProcess;
        _asynchronousSupport = asynchronousSupport;

        _maxSaveDuration =
            maxSaveDurationSeconds.HasValue
                ? TimeSpan.FromSeconds(maxSaveDurationSeconds.Value)
                : null;

        _backgroundPoller = _asynchronousSupport
            ? Task.Run(BackgroundPoll)
            : Task.CompletedTask;
    }

    public void Enqueue(
        TEntry entry)
    {
        if (_asynchronousSupport)
        {
            _flushInProgressEvent.WaitOne();

            _queue.Add(entry);
            return;
        }

        _processEntries(new[] { entry });
    }

    private void BackgroundPoll()
    {
        List<TEntry> poppedEntries = new((int)_maxEntriesPerProcess);

        try
        {
            var flushRequired = false;

            while (true)
            {
                while (poppedEntries.Count < _maxEntriesPerProcess)
                {
                    var success = _queue.TryTake(
                        out var poppedEntry,
                        100);

                    if (!flushRequired)
                        flushRequired = _shouldFlushARE.WaitOne(0);

                    if (_queue.IsCompleted)
                        break;

                    if (DateTime.Now - _lastSave >= _maxSaveDuration)
                        break;

                    // if no current elements and flush signaled, break out of popping loop which will force save entries and/or signal that flushing complete.
                    if (!success)
                        if (flushRequired)
                            break;
                        else
                            continue;

                    poppedEntries.Add(poppedEntry);
                }

                if (_queue.IsCompleted)
                    break;

                if (poppedEntries.Count <= 0)
                {
                    // no more entries to flush, signal that flush complete
                    if (flushRequired)
                    {
                        _flushedARE.Set();
                        flushRequired = false;
                    }

                    _lastSave = DateTime.Now;

                    continue;
                }

                _processEntries(poppedEntries);

                _lastSave = DateTime.Now;

                poppedEntries.Clear();
            }
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or OperationCanceledException)
        {
            // adding to queue finished
        }

        if (poppedEntries.Count > 0)
            _processEntries(poppedEntries);

        _flushedARE.Set();
    }

    public void Flush()
    {
        if (!_asynchronousSupport)
            return;

        _flushInProgressEvent.Reset();

        _flushedARE.Reset();
        _shouldFlushARE.Set();
        _flushedARE.WaitOne();

        _flushInProgressEvent.Reset();
    }

    public void Dispose()
    {
        if (_alreadyFinished)
            throw new Exception();

        _alreadyFinished = true;

        if (!_asynchronousSupport)
            return;

        _queue.CompleteAdding();
        _backgroundPoller.Wait();
        _queue.Dispose();

        _shouldFlushARE.Dispose();
        _flushedARE.Dispose();
    }
}
