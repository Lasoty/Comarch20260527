internal static class Program
{
    public static void Main()
    {
        var events = GenerateEvents(20_000);
        var counts = new Dictionary<string, int>();
        var errors = 0;

        Parallel.ForEach(events, logEvent =>
        {
            // Ten kod jest celowo problematyczny.
            if (!counts.ContainsKey(logEvent.Category))
            {
                counts[logEvent.Category] = 0;
            }

            counts[logEvent.Category]++;

            if (!logEvent.IsSuccess)
            {
                errors++;
            }
        });

        Console.WriteLine("Counts:");
        foreach (var item in counts.OrderBy(x => x.Key))
        {
            Console.WriteLine($"{item.Key,-12}: {item.Value}");
        }

        Console.WriteLine($"Errors: {errors}");
        Console.WriteLine($"Total counted: {counts.Values.Sum()}");
        Console.WriteLine($"Expected: {events.Count}");
    }

    private static List<LogEvent> GenerateEvents(int count)
    {
        var categories = new[] { "Orders", "Payments", "Invoices", "Users", "Reports" };
        var random = new Random(123);
        var result = new List<LogEvent>();

        for (var i = 0; i < count; i++)
        {
            result.Add(new LogEvent(
                Category: categories[random.Next(categories.Length)],
                IsSuccess: random.NextDouble() > 0.15));
        }

        return result;
    }
}

internal sealed record LogEvent(string Category, bool IsSuccess);