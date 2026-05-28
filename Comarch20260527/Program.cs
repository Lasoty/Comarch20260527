/*
    Refaktoryzacja kodu synchronicznego do async

    **Cel:** Usunąć `.Result`, wprowadzić `async/await`, poprawne nazewnictwo i `CancellationToken`.

    W wielu starszych projektach można spotkać kod, który wywołuje metodę asynchroniczną, ale blokuje się na wyniku przez `.Result` albo `.Wait()`. To jest antywzorzec sync-over-async. W aplikacjach webowych może prowadzić do blokowania wątków, a w aplikacjach UI lub starszym ASP.NET do deadlocków.
   
    W tym ćwiczeniu uczestnicy przepiszą kod tak, aby asynchroniczność była propagowana przez cały stos wywołań.
 */


internal static class Program
{
    public static void Main()
    {
        var service = new CustomerReportService(new FakeCustomerClient());
        var report = service.BuildReport(1);
        Console.WriteLine(report);
    }
}

internal sealed class CustomerReportService
{
    private readonly FakeCustomerClient _client;

    public CustomerReportService(FakeCustomerClient client)
    {
        _client = client;
    }

    public string BuildReport(int customerId)
    {
        var customer = _client.GetCustomerAsync(customerId).Result;
        var orders = _client.GetOrdersAsync(customerId).Result;

        return $"Customer: {customer.Name}, orders: {orders.Count}, total: {orders.Sum(x => x.Total):C}";
    }
}

internal sealed class FakeCustomerClient
{
    public async Task<Customer> GetCustomerAsync(int id)
    {
        await Task.Delay(300);
        return new Customer(id, "Anna Nowak");
    }

    public async Task<List<Order>> GetOrdersAsync(int customerId)
    {
        await Task.Delay(400);
        return
        [
            new Order(1, customerId, 120m),
            new Order(2, customerId, 250m),
            new Order(3, customerId, 80m)
        ];
    }
}

internal sealed record Customer(int Id, string Name);
internal sealed record Order(int Id, int CustomerId, decimal Total);