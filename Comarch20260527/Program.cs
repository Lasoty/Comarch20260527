/*
 * Symulator obsługi zamówień
 * 
 * **Cel:** Połączyć współdzielony stan, synchronizację, limitowanie współbieżności i statystyki thread-safe.
   
 * Wprowadzenie dla uczestników
 *
 * W miniprojekcie uczestnicy zbudują prosty symulator obsługi zamówień.
 * Każde zamówienie próbuje zarezerwować produkt z magazynu.
 * Magazyn jest współdzielonym stanem, więc musi być chroniony przed race condition.
 * Jednocześnie liczba równoległych operacji ma być ograniczona,
 * aby zasymulować ochronę zasobu zewnętrznego.
 *
 * Miniprojekt pokazuje, że poprawny kod współbieżny zwykle wymaga kilku decyzji naraz:
 * ochrony stanu, limitowania równoległości, obsługi błędów i zbierania statystyk.
 *
 * ### Zadania dla uczestników
 * 1. Uruchomić kod i wskazać potencjalne problemy współbieżności.
 * 2. Zabezpieczyć magazyn przez `lock`.
 * 3. Zastąpić liczniki `accepted` i `rejected` operacjami `Interlocked`.
 * 4. Dodać `SemaphoreSlim`, który ograniczy liczbę równoległych operacji obsługi zamówień do `8`.
 * 5. Sprawdzić, czy suma `accepted + rejected` jest równa liczbie zamówień.
 * 6. Sprawdzić, czy stan magazynu nigdy nie schodzi poniżej zera.
 */

internal static class Program
{
    public static async Task Main()
    {
        var inventory = new Dictionary<string, int>
        {
            ["Laptop"] = 20,
            ["Monitor"] = 35,
            ["Keyboard"] = 50
        };

        var inventoryLock = new object();
        using var concurrencyLimit = new SemaphoreSlim(8, 8);

        var orders = GenerateOrders(150);
        var accepted = 0;
        var rejected = 0;

        var tasks = orders.Select(async order =>
        {
            await concurrencyLimit.WaitAsync();
            try
            {
                await Task.Delay(Random.Shared.Next(5, 25));

                var wasAccepted = false;

                lock (inventoryLock)
                {
                    if (inventory.TryGetValue(order.Product, out var quantity) && quantity > 0)
                    {
                        inventory[order.Product] = quantity - 1;
                        wasAccepted = true;
                    }
                }

                if (wasAccepted)
                {
                    Interlocked.Increment(ref accepted);
                }
                else
                {
                    Interlocked.Increment(ref rejected);
                }
            }
            finally
            {
                concurrencyLimit.Release();
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine($"Accepted: {accepted}");
        Console.WriteLine($"Rejected: {rejected}");
        Console.WriteLine($"Total:    {accepted + rejected}");
        Console.WriteLine($"Orders:   {orders.Count}");
        Console.WriteLine();

        Console.WriteLine("Inventory:");
        foreach (var item in inventory.OrderBy(x => x.Key))
        {
            Console.WriteLine($"{item.Key,-10}: {item.Value}");
        }
    }

    private static List<Order> GenerateOrders(int count)
    {
        var products = new[] { "Laptop", "Monitor", "Keyboard" };
        var random = new Random(123);

        return Enumerable.Range(1, count)
            .Select(id => new Order(id, products[random.Next(products.Length)]))
            .ToList();
    }
}

internal sealed record Order(int Id, string Product);