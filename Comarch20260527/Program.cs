/* 
 * **Powiązany slajd:** Ćwiczenie: worker produkcyjny  
   **Cel:** Zasymulować produkcyjny worker: kolejka, ograniczenie równoległości, anulowanie, błędy i graceful shutdown.
   
   ### Wprowadzenie dla uczestników
   
   W prawdziwej aplikacji webowej praca długotrwała często trafia do kolejki, a następnie jest przetwarzana przez worker. 
   Ponieważ ćwiczenia są realizowane w aplikacji konsolowej, zasymulujemy ten model bez tworzenia pełnej aplikacji ASP.NET Core. 
   Użyjemy `Channel<T>` jako kolejki i klasy `QueuedWorker` jako uproszczonego workera.

   Zadania dla uczestników
   
   1. Wydzielić klasę `QueuedWorker`.
   2. Użyć bounded channel jako kolejki.
   3. Dodać ograniczenie równoległości przetwarzania do `4`.
   4. Dodać `CancellationTokenSource`, który zasymuluje zamykanie aplikacji.
   5. Obsłużyć błędy pojedynczych zadań.
   6. Zaimplementować kontrolowane zakończenie pracy.

*/

using System.Threading.Channels;

internal static class Program
{
    public static async Task Main()
    {
    }
}

