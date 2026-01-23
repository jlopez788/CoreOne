using CoreOne;
using NUnit.Framework;

namespace Tests;

public class AsyncTaskQueueTests
{
    [Test]
    public async Task Enqueue_ExecutesAction()
    {
        var queue = new AsyncTaskQueue();
        var executed = false;
        
        await queue.Enqueue(() => executed = true);
        
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task Enqueue_ExecutesAsyncAction()
    {
        var queue = new AsyncTaskQueue();
        var executed = false;
        
        await queue.Enqueue(async () => {
            await Task.Delay(10);
            executed = true;
        });
        
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task Enqueue_ReturnsResult()
    {
        var queue = new AsyncTaskQueue();
        
        var result = await queue.Enqueue(async () => {
            await Task.Delay(10);
            return 42;
        });
        
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Enqueue_ExecutesInOrder()
    {
        var queue = new AsyncTaskQueue(concurrency: 1);
        var results = new List<int>();
        
        var task1 = queue.Enqueue(async () => {
            await Task.Delay(50);
            results.Add(1);
        });
        
        var task2 = queue.Enqueue(async () => {
            await Task.Delay(10);
            results.Add(2);
        });
        
        var task3 = queue.Enqueue(async () => {
            results.Add(3);
        });
        
        await Task.WhenAll(task1, task2, task3);
        
        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task Enqueue_MultipleWorkers_ExecuteConcurrently()
    {
        var queue = new AsyncTaskQueue(concurrency: 3);
        var startTimes = new List<DateTime>();
        var sync = new object();
        
        var tasks = Enumerable.Range(0, 3).Select(i => queue.Enqueue(async () => {
            lock (sync)
            {
                startTimes.Add(DateTime.Now);
            }
            await Task.Delay(100);
        })).ToList();
        
        await Task.WhenAll(tasks);
        
        // All 3 should start roughly at the same time (within reasonable margin)
        var timeSpan = startTimes.Max() - startTimes.Min();
        Assert.That(timeSpan.TotalMilliseconds, Is.LessThan(500));
    }

    [Test]
    public async Task Enqueue_CancellationToken_CancelsTask()
    {
        var queue = new AsyncTaskQueue();
        var cts = new CancellationTokenSource();
        var executed = false;
        
        var task = queue.Enqueue(async () => {
            await Task.Delay(1000);
            executed = true;
        }, cts.Token);
        
        cts.Cancel();
        
        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.That(executed, Is.False);
    }

    [Test]
    public async Task Enqueue_Exception_PropagatesException()
    {
        var queue = new AsyncTaskQueue();
        
        var task = queue.Enqueue(async () => {
            await Task.Delay(10);
            throw new InvalidOperationException("Test exception");
        });
        
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        Assert.That(ex!.Message, Is.EqualTo("Test exception"));
    }

    [Test]
    public async Task Enqueue_MultipleExceptions_IsolatesFailures()
    {
        var queue = new AsyncTaskQueue(concurrency: 2);
        
        var task1 = queue.Enqueue(async () => {
            await Task.Delay(10);
            throw new InvalidOperationException("Error 1");
        });
        
        var task2 = queue.Enqueue(async () => {
            await Task.Delay(10);
            return 42;
        });
        
        Assert.ThrowsAsync<InvalidOperationException>(async () => await task1);
        var result = await task2;
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Enqueue_EmptyQueue_CompletesImmediately()
    {
        var queue = new AsyncTaskQueue();
        var executed = false;
        
        await queue.Enqueue(() => executed = true);
        
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task Enqueue_MultipleQueues_Independent()
    {
        var queue1 = new AsyncTaskQueue();
        var queue2 = new AsyncTaskQueue();
        var results1 = new List<int>();
        var results2 = new List<int>();
        
        var task1 = queue1.Enqueue(() => results1.Add(1));
        var task2 = queue2.Enqueue(() => results2.Add(2));
        
        await Task.WhenAll(task1, task2);
        
        Assert.That(results1, Is.EqualTo(new[] { 1 }));
        Assert.That(results2, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public async Task Enqueue_HighConcurrency_HandlesLoad()
    {
        var queue = new AsyncTaskQueue(concurrency: 10);
        var counter = 0;
        var sync = new object();
        
        var tasks = Enumerable.Range(0, 100).Select(i => queue.Enqueue(() => {
            lock (sync)
            {
                counter++;
            }
        })).ToList();
        
        await Task.WhenAll(tasks);
        
        Assert.That(counter, Is.EqualTo(100));
    }
}
