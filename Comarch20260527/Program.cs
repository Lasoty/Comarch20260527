// https://github.com/Lasoty/Comarch20260527

using System.Diagnostics;

const int WorkItems = 5000;

Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}.");
Console.WriteLine();

RunWithThreads();
RunWithThreadPool();
await RunWithTaskAsync();

async Task RunWithTaskAsync()
{
    Console.WriteLine("=== Task.Run ===");
    var stopwatch = Stopwatch.StartNew();
    var tasks = new List<Task>();

    for (int i = 0; i < WorkItems; i++)
    {
        var jobId = i;
        tasks.Add(Task.Run(() => SimulatedWork(jobId, "Task.Run")));
    }

    await Task.WhenAll(tasks);
    Console.WriteLine($"Task.Run finished in {stopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


void RunWithThreadPool()
{
    Console.WriteLine("=== Threadpool ===");
    var stopwatch = Stopwatch.StartNew();
    using var countdown = new CountdownEvent(WorkItems);

    for (int i = 0; i < WorkItems; i++)
    {
        var jobId = i;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                SimulatedWork(jobId, "ThreadPool");
            }
            finally
            {
                countdown.Signal();
            }
        });
    }

    countdown.Wait();
    Console.WriteLine($"ThreadPool finished in {stopwatch.ElapsedMilliseconds} ms.");
    Console.WriteLine();
}


void RunWithThreads()
{
    Console.WriteLine("=== Thread ===");
    var stopwatch = Stopwatch.StartNew();
    var threads = new List<Thread>();

    for (int i = 0; i < WorkItems; i++)
    {
        var jobId = i;
        var thread = new Thread(() =>
        {
            SimulatedWork(jobId, "Thread");
        });
        threads.Add(thread);
        thread.Start();
    }

    foreach (Thread thread in threads)
    {
        thread.Join();
    }

    Console.WriteLine($"Thread finished in {stopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine();
}

void SimulatedWork(int jobId, string mechanism)
{
    Console.WriteLine($"{mechanism,-10} job {jobId,2} started on thread {Environment.CurrentManagedThreadId}");
    Thread.Sleep(250);
    Console.WriteLine($"{mechanism,-10} job {jobId,2} ended on thread {Environment.CurrentManagedThreadId}");
}