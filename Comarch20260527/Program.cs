/*
   Powiązany slajd: Ćwiczenie: równoległe przetwarzanie danych
   Cel: Porównać wersję sekwencyjną, Parallel.ForEach i Parallel.ForEachAsync oraz wpływ MaxDegreeOfParallelism.

   Wprowadzenie dla uczestników
   Nie każda równoległość przyspiesza aplikację.
   W tym ćwiczeniu uczestnicy porównają kilka wariantów przetwarzania tej samej kolekcji.
   Celem jest zobaczenie, że Parallel pasuje do pracy CPU-bound, a Parallel.ForEachAsync może być
   czytelnym sposobem kontrolowania współbieżności przy operacjach asynchronicznych.

*/

using System.Diagnostics;

internal static class Program
{
    public static async Task Main()
    {
        var numbers = Enumerable.Range(35_000, 2_000).ToArray();


    }
}