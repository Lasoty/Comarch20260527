/* 
 * **Powiązany slajd:** Ćwiczenie: worker produkcyjny  
   **Cel:** Zasymulować produkcyjny worker: kolejka, ograniczenie równoległości, anulowanie, błędy i graceful shutdown.
   
   ### Wprowadzenie dla uczestników
   
   W prawdziwej aplikacji webowej praca długotrwała często trafia do kolejki, a następnie jest przetwarzana przez worker. 
   Ponieważ ćwiczenia są realizowane w aplikacji konsolowej, zasymulujemy ten model bez tworzenia pełnej aplikacji ASP.NET Core. 
   Użyjemy `Channel<T>` jako kolejki i klasy `QueuedWorker` jako uproszczonego workera.

   Zadania dla uczestników
   
   1. Wydzielić klasę `QueuedWorker`.
   2. Użyć bounded channel jako kolejki.
   3. Dodać ograniczenie równoległości przetwarzania do `4`.
   4. Dodać `CancellationTokenSource`, który zasymuluje zamykanie aplikacji.
   5. Obsłużyć błędy pojedynczych zadań.
   6. Zaimplementować kontrolowane zakończenie pracy.

*/

using System.ComponentModel;
using System.Threading.Channels;

internal static class Program
{
    public static async Task Main()
    {
        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        var worker = new QueuedWorker(maxParallelism: 4, capacity: 20);
        var workerTask = worker.RunAsync(shutdownCts.Token);

        for (int i = 1; i <= 140; i++)
        {
            var accepted = await worker.EnqueueAsync(new WorkItem(i), shutdownCts.Token);
            Console.WriteLine(accepted ? "Enqueued" : "Rejected");
        }

        worker.Complete();

        try
        {
            await workerTask;
        }
        catch (OperationCanceledException ce)
        {
            Console.WriteLine("Worker stoped because application is shutting down.");
        }

        Console.WriteLine("Application finished.");
    }
}

internal sealed class QueuedWorker
{
    private readonly Channel<WorkItem> _queue;
    private readonly int _maxParallelism;

    public QueuedWorker(int maxParallelism, int capacity)
    {
        _maxParallelism = maxParallelism;
        _queue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task<bool> EnqueueAsync(WorkItem item, CancellationToken cancellationToken)
    {
        try
        {
            await _queue.Writer.WriteAsync(item, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public void Complete()
    {
        _queue.Writer.TryComplete();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_maxParallelism, _maxParallelism);
        var runningTasks = new List<Task>();

        await foreach (var item in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ProcessAsync(item, cancellationToken);
                    Console.WriteLine($"Processed {item.Id}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.WriteLine($"Item {item.Id} failed: {ex.Message}.");
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            runningTasks.Add(task);
            runningTasks.RemoveAll(t => t.IsCompleted);
        }

        await Task.WhenAll(runningTasks);
    }

    private async Task ProcessAsync(WorkItem item, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(150, 500), cancellationToken);

        if (item.Id % 13 == 0)
        {
            throw new InvalidOperationException("Simulated worker error");
        }
    }
}

internal record WorkItem(int Id);
