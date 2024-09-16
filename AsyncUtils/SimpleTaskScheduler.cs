using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class SimpleTaskScheduler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _taskQueue;
    
    //
    private int _isProcessing; // Use an int to track the processing state atomically

    public SimpleTaskScheduler(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency); // Limit the number of concurrent tasks
        _taskQueue = new ConcurrentQueue<Func<CancellationToken, Task>>(); // Thread-safe queue to hold the tasks
        _isProcessing = 0;
    }

    // Method to schedule a new task
    public void EnqueueTask(Func<CancellationToken, Task> task, CancellationToken cancellationToken)
    {
        _taskQueue.Enqueue(task); // Enqueue the task

        // Start processing the queue if not already processing
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
        {
            _ = ProcessQueueAsync(cancellationToken); // Process the queue asynchronously
        }
    }

    // Private method that processes the queued tasks
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_taskQueue.TryDequeue(out var taskToRun))
                {
                    // Ensure that the number of concurrent tasks is limited
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // Execute the task and pass the cancellation token
                        await taskToRun(cancellationToken);
                    }
                    finally
                    {
                        _semaphore.Release(); // Release the semaphore after task completion
                    }
                }
                else
                {
                    // No more tasks to process; exit the loop
                    Interlocked.Exchange(ref _isProcessing, 0);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Task processing was canceled.");
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0); // Ensure we reset the processing flag if cancellation occurs
        }
    }
}
