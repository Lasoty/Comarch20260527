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

        try
        {
            await importer.ImportAsync(records);
            Console.WriteLine("Import completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Import failed: {ex.Message}");
        }
    }
}

internal sealed class Importer
{
    public async Task ImportAsync(IReadOnlyList<ImportRecord> records)
    {
        foreach (var record in records)
        {
            await ValidateAsync(record);
            await SaveAsync(record);
            Console.WriteLine($"Imported record {record.Id}");
        }
    }

    private static async Task ValidateAsync(ImportRecord record)
    {
        await Task.Delay(80);

        if (string.IsNullOrWhiteSpace(record.Value))
        {
            throw new InvalidOperationException($"Record {record.Id} is invalid.");
        }
    }

    private static async Task SaveAsync(ImportRecord record)
    {
        await Task.Delay(120);
    }
}

internal sealed record ImportRecord(int Id, string Value);