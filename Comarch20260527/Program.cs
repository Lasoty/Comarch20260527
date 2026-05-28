/*
 * Wiele usług z timeoutem i raportem błędów
 *
 * **Cel:** Użyć `Task.WhenAny`, timeoutów i obsługi błędów częściowych. 
 *
 * W praktycznych aplikacjach często odpytywane jest kilka usług zewnętrznych.
 * Nie chcemy, aby błąd jednej usługi zatrzymał cały proces.
 * Nie chcemy też czekać bez końca na najwolniejszą usługę.
 * W tym ćwiczeniu uczestnicy zaimplementują mechanizm, który odpytuje kilka źródeł,
 * przetwarza wyniki w kolejności zakończenia i tworzy raport.
 */


internal static class Program
{
    public static async Task Main()
    {
        var services = new[]
        {
            new ExternalService("Catalog", 700, false),
            new ExternalService("Pricing", 1200, false),
            new ExternalService("Availability", 2500, false),
            new ExternalService("Recommendations", 800, true),
            new ExternalService("Reviews", 3500, false)
        };

        Console.WriteLine("Starting calls...");

        foreach (var service in services)
        {
            var result = await service.CallAsync(CancellationToken.None);
            Console.WriteLine($"{service.Name}: {result}");
        }
    }
}

internal sealed class ExternalService
{
    public string Name { get; }
    private readonly int _delayMs;
    private readonly bool _shouldFail;

    public ExternalService(string name, int delayMs, bool shouldFail)
    {
        Name = name;
        _delayMs = delayMs;
        _shouldFail = shouldFail;
    }

    public async Task<string> CallAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_delayMs, cancellationToken);

        if (_shouldFail)
        {
            throw new InvalidOperationException($"{Name} failed.");
        }

        return $"Response from {Name}";
    }
}