/*

Ćwiczenie: anulowalny import
Cel: Dodać CancellationToken do procesu importu i poprawnie odróżnić anulowanie od błędu.

Długotrwałe procesy, takie jak import danych, muszą wspierać anulowanie. Użytkownik może przerwać operację, żądanie HTTP może zostać anulowane, a aplikacja może się zamykać. Kod powinien reagować na CancellationToken i kończyć pracę w kontrolowany sposób.

Zadania dla uczestników
   1. Dodać CancellationToken do ImportAsync, ValidateAsync i SaveAsync.
   2. Dodać CancellationTokenSource, który anuluje import po 2 sekundach.
   3. W pętli importu użyć ThrowIfCancellationRequested.
   4. Obsłużyć OperationCanceledException osobno od innych wyjątków.
   5. Upewnić się, że anulowanie nie jest logowane jako awaria biznesowa.

 */



internal static class Program
{
    public static async Task Main()
    {
        var importer = new Importer();
        var records = Enumerable.Range(1, 50).Select(id => new ImportRecord(id, $"Value {id}")).ToList();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            await importer.ImportAsync(records, cts.Token);
            Console.WriteLine("Import completed.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Import was cancelled. This is not treated as a failure.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Import failed: {ex.Message}");
        }
    }
}

internal sealed class Importer
{
    public async Task ImportAsync(IReadOnlyList<ImportRecord> records, CancellationToken cancellationToken)
    {
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ValidateAsync(record, cancellationToken);
            await SaveAsync(record, cancellationToken);
            Console.WriteLine($"Imported record {record.Id}");
        }
    }

    private static async Task ValidateAsync(ImportRecord record, CancellationToken cancellationToken)
    {
        await Task.Delay(80, cancellationToken);

        if (string.IsNullOrWhiteSpace(record.Value))
        {
            throw new InvalidOperationException($"Record {record.Id} is invalid.");
        }
    }

    private static async Task SaveAsync(ImportRecord record, CancellationToken cancellationToken)
    {
        await Task.Delay(120, cancellationToken);
    }
}

internal sealed record ImportRecord(int Id, string Value);