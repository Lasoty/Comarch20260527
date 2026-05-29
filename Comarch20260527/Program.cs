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


internal static class Program
{
    public static async Task Main()
    {
        var jobs = Enumerable.Range(1, 30)
            .Select(id => new Job(id, $"https://example.local/data/{id}"))
            .ToList();

        Console.WriteLine("Jobs to process:");
        foreach (var job in jobs)
        {
            Console.WriteLine($"Job {job.Id}: {job.Url}");
        }

        Console.WriteLine();
        Console.WriteLine("TODO: Build processing pipeline.");
        await Task.CompletedTask;
    }
}

internal sealed record Job(int Id, string Url);