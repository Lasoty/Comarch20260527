/*
Projekt końcowy
   **Cel:** Zbudować kompletny mini-system łączący asynchroniczne I/O, przetwarzanie CPU-bound, kanały, limity, anulowanie i raportowanie.

   ### Wprowadzenie dla uczestników

   Projekt końcowy łączy najważniejsze tematy całego szkolenia. System ma przyjąć listę zadań, pobrać dane asynchronicznie, wykonać część CPU-bound równolegle, zapisać wyniki i przygotować raport. Przepływ powinien być oparty o `Channel` lub TPL Dataflow. W tym skrypcie rozwiązanie referencyjne używa `Channel`, ponieważ jest dostępny bez dodatkowych pakietów w nowoczesnym .NET.

   Architektura:

   ```text
   Input -> Channel<Job> -> Downloader -> Channel<RawData> -> Processor -> Channel<ProcessedData> -> Writer -> Report
   ```
   
   Zadania dla uczestników
   
   1. Utworzyć trzy bounded channels:
      - `Channel<Job>` dla wejścia,
      - `Channel<RawData>` dla danych pobranych,
      - `Channel<ProcessedData>` dla wyników przetwarzania.
   2. Utworzyć etap downloadera, który pobiera dane asynchronicznie.
   3. Utworzyć etap procesora, który wykonuje pracę CPU-bound.
   4. Utworzyć etap writera, który zapisuje wyniki asynchronicznie.
   5. Dodać limity równoległości dla każdego etapu.
   6. Dodać `CancellationToken` i globalny timeout.
   7. Obsłużyć błędy pojedynczych zadań bez zatrzymania całego pipeline’u.
   8. Przygotować raport końcowy.

 */


using System.Collections.Concurrent;
using System.Threading.Channels;

internal static class Program
{
    public static async Task Main()
    {
        var jobs = Enumerable.Range(1, 30)
            .Select(id => new Job(id, $"https://example.local/data/{id}"))
            .ToList();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var pipeline = new ProcessingPipeline(
            downloaders: 4,
            processors: Environment.ProcessorCount,
            writers: 3,
            channelCapacity: 10);

        var report = await pipeline.RunAsync(jobs, cts.Token);

        Console.WriteLine();
        Console.WriteLine("Final report:");
        Console.WriteLine($"Input jobs:       {report.InputJobs}");
        Console.WriteLine($"Downloaded:       {report.Downloaded}");
        Console.WriteLine($"Processed:        {report.Processed}");
        Console.WriteLine($"Written:          {report.Written}");
        Console.WriteLine($"Failed:           {report.Failures.Count}");

        if (report.Failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Failures:");
            foreach (var failure in report.Failures.OrderBy(x => x.JobId))
            {
                Console.WriteLine($"Job {failure.JobId}: {failure.Stage} - {failure.Message}");
            }
        }
    }
}

internal sealed class ProcessingPipeline
{
    private readonly int _downloaders;
    private readonly int _processors;
    private readonly int _writers;
    private readonly int _channelCapacity;

    public ProcessingPipeline(int downloaders, int processors, int writers, int channelCapacity)
    {
        _downloaders = downloaders;
        _processors = processors;
        _writers = writers;
        _channelCapacity = channelCapacity;
    }

    public async Task<PipelineReport> RunAsync(IReadOnlyList<Job> jobs, CancellationToken cancellationToken)
    {
        var jobChannel = Channel.CreateBounded<Job>(_channelCapacity);
        var rawChannel = Channel.CreateBounded<RawData>(_channelCapacity);
        var processedChannel = Channel.CreateBounded<ProcessedData>(_channelCapacity);

        var failures = new ConcurrentBag<PipelineFailure>();
        var downloaded = 0;
        var processed = 0;
        var written = 0;

        var producer = ProduceJobsAsync(jobs, jobChannel.Writer, cancellationToken);

        var downloadTasks = Enumerable.Range(1, _downloaders)
            .Select(workerId => DownloadWorkerAsync(
                workerId,
                jobChannel.Reader,
                rawChannel.Writer,
                failures,
                () => Interlocked.Increment(ref downloaded),
                cancellationToken))
            .ToList();

        var processorTasks = Enumerable.Range(1, _processors)
            .Select(workerId => ProcessorWorkerAsync(
                workerId,
                rawChannel.Reader,
                processedChannel.Writer,
                failures,
                () => Interlocked.Increment(ref processed),
                cancellationToken))
            .ToList();

        var writerTasks = Enumerable.Range(1, _writers)
            .Select(workerId => WriterWorkerAsync(
                workerId,
                processedChannel.Reader,
                failures,
                () => Interlocked.Increment(ref written),
                cancellationToken))
            .ToList();

        await producer;
        jobChannel.Writer.Complete();

        await Task.WhenAll(downloadTasks);
        rawChannel.Writer.Complete();

        await Task.WhenAll(processorTasks);
        processedChannel.Writer.Complete();

        await Task.WhenAll(writerTasks);

        return new PipelineReport(
            InputJobs: jobs.Count,
            Downloaded: downloaded,
            Processed: processed,
            Written: written,
            Failures: failures.ToList());
    }

    private static async Task ProduceJobsAsync(
        IEnumerable<Job> jobs,
        ChannelWriter<Job> writer,
        CancellationToken cancellationToken)
    {
        foreach (var job in jobs)
        {
            await writer.WriteAsync(job, cancellationToken);
            Console.WriteLine($"Input: job {job.Id}");
        }
    }

    private static async Task DownloadWorkerAsync(
        int workerId,
        ChannelReader<Job> reader,
        ChannelWriter<RawData> writer,
        ConcurrentBag<PipelineFailure> failures,
        Action onDownloaded,
        CancellationToken cancellationToken)
    {
        await foreach (var job in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var raw = await DownloadAsync(job, cancellationToken);
                await writer.WriteAsync(raw, cancellationToken);
                onDownloaded();
                Console.WriteLine($"Downloader {workerId}: job {job.Id}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(new PipelineFailure(job.Id, "Download", ex.Message));
            }
        }
    }

    private static async Task ProcessorWorkerAsync(
        int workerId,
        ChannelReader<RawData> reader,
        ChannelWriter<ProcessedData> writer,
        ConcurrentBag<PipelineFailure> failures,
        Action onProcessed,
        CancellationToken cancellationToken)
    {
        await foreach (var raw in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var processed = Process(raw);
                await writer.WriteAsync(processed, cancellationToken);
                onProcessed();
                Console.WriteLine($"Processor {workerId}: job {raw.JobId}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(new PipelineFailure(raw.JobId, "Process", ex.Message));
            }
        }
    }

    private static async Task WriterWorkerAsync(
        int workerId,
        ChannelReader<ProcessedData> reader,
        ConcurrentBag<PipelineFailure> failures,
        Action onWritten,
        CancellationToken cancellationToken)
    {
        await foreach (var data in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await SaveAsync(data, cancellationToken);
                onWritten();
                Console.WriteLine($"Writer {workerId}: job {data.JobId}, score {data.Score}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(new PipelineFailure(data.JobId, "Write", ex.Message));
            }
        }
    }

    private static async Task<RawData> DownloadAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(80, 220), cancellationToken);

        if (job.Id % 11 == 0)
        {
            throw new InvalidOperationException("Simulated download error.");
        }

        var payload = string.Join('|', Enumerable.Range(1, 20).Select(i => job.Id * i));
        return new RawData(job.Id, payload);
    }

    private static ProcessedData Process(RawData raw)
    {
        if (raw.JobId % 17 == 0)
        {
            throw new InvalidOperationException("Simulated processing error.");
        }

        var numbers = raw.Payload
            .Split('|')
            .Select(int.Parse)
            .ToArray();

        var score = 0;
        foreach (var number in numbers)
        {
            for (var i = 0; i < 5_000; i++)
            {
                score = unchecked(score + ((number * 31) ^ i));
            }
        }

        return new ProcessedData(raw.JobId, Math.Abs(score % 10_000));
    }

    private static async Task SaveAsync(ProcessedData data, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(50, 160), cancellationToken);

        if (data.JobId % 19 == 0)
        {
            throw new InvalidOperationException("Simulated write error.");
        }
    }
}

internal sealed record Job(int Id, string Url);
internal sealed record RawData(int JobId, string Payload);
internal sealed record ProcessedData(int JobId, int Score);
internal sealed record PipelineFailure(int JobId, string Stage, string Message);
internal sealed record PipelineReport(
    int InputJobs,
    int Downloaded,
    int Processed,
    int Written,
    List<PipelineFailure> Failures);