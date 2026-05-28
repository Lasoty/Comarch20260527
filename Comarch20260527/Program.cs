/*
 * Połączyć Task.WhenAll, timeout, anulowanie i raportowanie błędów.
 *
 * Ten miniprojekt jest podsumowaniem dnia drugiego. Uczestnicy mają zbudować importer, który pobiera dane z kilku źródeł, robi to asynchronicznie, kontroluje timeout, obsługuje anulowanie i nie przerywa całego procesu przez pojedynczy błąd.
 *
 * Zadania:
 * 1. Dodać `CancellationToken` do `ImportAsync`.
   2. Ustawić globalny timeout `3 sekundy`.
   3. Uruchomić import ze wszystkich źródeł współbieżnie.
   4. Obsłużyć błędy pojedynczych źródeł bez przerywania całości.
   5. Przygotować raport końcowy: sukcesy, błędy, anulowania, liczba rekordów.
   6. Nie używać `.Result` ani `.Wait()`.
 *
 */


internal static class Program
{
    public static async Task Main()
    {
        var sources = new[]
        {
            new DataSource("CRM", 800, false),
            new DataSource("ERP", 1400, false),
            new DataSource("Warehouse", 2600, false),
            new DataSource("Legacy", 1000, true),
            new DataSource("Analytics", 4200, false)
        };

        var importer = new DataImporter();
        await importer.ImportAsync(sources);
    }
}

internal sealed class DataImporter
{
    public async Task ImportAsync(IEnumerable<DataSource> sources)
    {
        foreach (var source in sources)
        {
            var records = await source.LoadAsync(CancellationToken.None);
            Console.WriteLine($"{source.Name}: imported {records.Count} records");
        }
    }
}

internal sealed class DataSource
{
    public string Name { get; }
    private readonly int _delayMs;
    private readonly bool _shouldFail;

    public DataSource(string name, int delayMs, bool shouldFail)
    {
        Name = name;
        _delayMs = delayMs;
        _shouldFail = shouldFail;
    }

    public async Task<List<ImportedRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_delayMs, cancellationToken);

        if (_shouldFail)
        {
            throw new InvalidOperationException($"Source {Name} failed.");
        }

        return Enumerable.Range(1, 5)
            .Select(i => new ImportedRecord($"{Name}-{i}", Random.Shared.Next(10, 100)))
            .ToList();
    }
}

internal sealed record ImportedRecord(string ExternalId, int Value);