using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace SampleAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ParalelController : ControllerBase
  {

    [HttpGet("paralelFor")]
    public IActionResult ParalelFor()
    {
      // Parallel LINQ dışındaki durumlarda ise Paralel sınıfını kullanıypruz.
      // LINQ ParalelQuery 


      var sp1 = Stopwatch.StartNew();

      // Paralel olarak birden fazla thread ile 0,100 arasında işlem yapacağız.
      Parallel.For(0, 200000000, (item) =>
      {
        // bunun içine yazılan kodları thread bazlı multi thread uygular.
        // Console.Out.WriteLine("Thread Id" + Thread.CurrentThread.ManagedThreadId);
        double d = Math.Sqrt(item) * Math.Pow(item,2);
        // Console.Out.WriteLine($"{item} karakök {d}");
      });
      sp1.Stop();
      Console.Out.WriteLine("Toplam Süre Paralel " + sp1.ElapsedMilliseconds + " ms");

      var sp2 = Stopwatch.StartNew();

      for (int i = 0; i < 200000000; i++)
      {
        // Console.Out.WriteLine("Thread Id " + Thread.CurrentThread.ManagedThreadId);
        double d = Math.Sqrt(i) * Math.Pow(i, 2); ;
        // Console.Out.WriteLine($"{i} karakök {d}");
      }
      sp2.Stop();
      Console.Out.WriteLine("Toplam Süre Senkron " + sp2.ElapsedMilliseconds + " ms");


      return Ok();
    }

    // Not: Paralel programlama birbirleri ile alaklı olan işlemlerden ziyade daha çok birbirinden bağımsız hesaplanan kod blokları için performanslı bir çözüm. diğer türlü race condition gibi bir durum ile karşılaşabiliriz.

    [HttpGet("paralelInvoke")]
    public IActionResult ParalelInvoke()
    {
      Action action = () =>
      {
        Thread.Sleep(3000);
        Console.Out.WriteLine("Action 1 " + Thread.CurrentThread.ManagedThreadId);
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal; // 2
      };

      Action action1 = () =>
      {
        Thread.Sleep(3000);
        Console.Out.WriteLine("Action 2 " + Thread.CurrentThread.ManagedThreadId);
        Thread.CurrentThread.Priority = ThreadPriority.Highest; // 1
        // Not: Sistemin darboğaz girdiği kaynakları yeterince rahat tüketemediği durumlarda ilk öncelik Highest,BelowNormal,Lowest
      };

      Action action2 = () =>
      {
        Thread.Sleep(3000);
        Console.Out.WriteLine("Action 3 " + Thread.CurrentThread.ManagedThreadId);
        Thread.CurrentThread.Priority = ThreadPriority.Lowest; // 3
      };

      Parallel.Invoke(action, action1, action2);

      return Ok();

    }


    [HttpGet("raceCondition")]
    public IActionResult RaceCondition()
    {
      int sum = 0;
      object lockObj = new object();

      Parallel.ForEach(Enumerable.Range(0, 1000000), (item) =>
      {

        //sum++;
        //lock (lockObj)
        //{
        //  // thread safe işlemler yapabiliriz.
        //  sum++;
        //}

        // Thread Safe çalışan bir sınıf
        Interlocked.Increment(ref sum);


      });

      Console.Out.WriteLine("Toplam: " + sum);


      return Ok();
    }

    [HttpGet("threadSafeCollections")]
    public IActionResult ThreadSafeCollections()
    {
      List<int> ints = new(); // Normal Collection ifadeleri Thread Safe Değil
      ConcurrentBag<int> bags = new();
      BlockingCollection<int> bs = new(500000);


      // Concurent Collection Race Condition durumu yaşamıyoruz.
      // Kapasite problemleride yaşamayız.
      // BlockingCollection,
      // ConcurentDictionary
      // ConcurentBag
      // ConcurentStack (LIFO)
      // ConcurentQuee (FIFO)


      // capacity was less than the current size. 
      // bu kadar büyük veri kümesine sahip listelerde kapasite problemleri yaşıyoruz.
      // Not: örnekteki ifadeyi 1000000 için deniyelim

      Parallel.ForEach(Enumerable.Range(0, 1000000000), (item) =>
      {
        ints.Add(item);
        //bags.Add(item);

        //if (bs.TryAdd(item)) // Kontrollü ekleme işlemi
        //{

        //}

      });

      Console.Out.WriteLine("1. bs Count " + bs.Count);
      Console.Out.WriteLine("1. ints Count " + ints.Count);

      Parallel.ForEach(Enumerable.Range(0, 1000000), (item) =>
      {
        ints.Remove(item);
        while (bs.TryTake(out item))
        {

        }

      });

      Console.Out.WriteLine("2. bs Count" + bs.Count);
      Console.Out.WriteLine("1. ints Count " + ints.Count);


      //Console.Out.WriteLine("Total Count " +  ints.Count);
      Console.Out.WriteLine("Bag Total Count" + bags.Count);

      return Ok();
    }






    [HttpGet("threadSafeCollectionsV2")]
    public IActionResult ThreadSafeCollectionsV2()
    {

      // Not: Birbirinden bağımsız olan multi thread yada asenkron işlemlerde Concurent Bag tercih edelim.
      ConcurrentBag<int> bags = new();
      // Not: Aynı kolleksiyona iki farklı thread yada task işlem paralel yada asenkron işlem yapacak ise kolleksiyon içerisindeki değerlerin kontrollü eklenip çıkarılması açısından ve kapasite kontrollerin yapılması açısından daha doğru bir kullanım.

      BlockingCollection<int> bs = new(); // Daha yava ekleme yapacak.


      ConcurrentDictionary<string, int> dc = new();
      

      Stopwatch sp = Stopwatch.StartNew();
      Parallel.ForEach(Enumerable.Range(0, 1000000), (item) =>
      {

        if (bs.TryAdd(item)) // Kontrollü ekleme işlemi
        {

        }

      });
      sp.Stop();

      Stopwatch sp1 = Stopwatch.StartNew();

      Console.Out.WriteLine("Blocking Collection Count time ms" + sp.ElapsedMilliseconds);

      Parallel.ForEach(Enumerable.Range(0, 1000000), (item) =>
      {
        bags.Add(item);

      });
      sp1.Stop();

      Console.Out.WriteLine("ConcurentBag ms" + sp1.ElapsedMilliseconds);

      return Ok();
    }


    [HttpGet("ParalelForForeachAsync")]
    public async Task<IActionResult> ParalelForForeachAsync(CancellationToken token)
    {

      CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
      var token1 = cancellationTokenSource.Token;


      //// 1.yazım şekli   Parallel.For Main Thread bloke etmez
      //var task =  Task.Run(() =>
      //{
      //  // Task.Run içerisindeki kod bloğu asenkron bir şekilde çalışır.
      //  Parallel.For(0, 1000000, (item) =>
      //  {
      //    Console.Out.WriteLine("Thread Id" + Thread.CurrentThread.ManagedThreadId);
      //    Thread.Sleep(1000);
      //  });

      //});



      // Paralel işlemleri iptal etmek için token kullanabiliriz. Paralel Optionsdan yararlanılır.
      var parallelOptions = new ParallelOptions();
      parallelOptions.CancellationToken = token;
      parallelOptions.TaskScheduler = TaskScheduler.Default;
      parallelOptions.MaxDegreeOfParallelism = 6; // Maksimum işlemi kaç thread'e ayırıcağımı belirlediğimiz kod.
      // Not: Debug modda çalıştırıldığında 1 thread çalışır. Release modda çalıştırıldığında 6 thread çalışır.

      // Asekron bir kod bloğunda task schedular kullanım şekli
      //TaskFactory taskFactory = new TaskFactory(TaskScheduler.Default);
      //await taskFactory.StartNew(() =>
      //{
      //  Console.Out.WriteLineAsync("Deneme");
      //});


      try
      {
        await Parallel.ForAsync(0, 500000, parallelOptions, async (item, token) =>
        {


          if (parallelOptions.CancellationToken.IsCancellationRequested)
          {
            //await Console.Out.WriteLineAsync("Operation was Canceled");
            token.ThrowIfCancellationRequested();
          }


          Console.Out.WriteLine("Thread Id" + Thread.CurrentThread.ManagedThreadId);
          await Task.Delay(1000);
        });

      }
      catch (OperationCanceledException ex)
      {
        await Console.Out.WriteLineAsync(ex.Message);
        throw;
      }

     
  


      //Parallel.For(0, 1000000, (item) =>
      //{
      //  Console.Out.WriteLine("Not Async Thread Id" + Thread.CurrentThread.ManagedThreadId);
      //  Thread.Sleep(100);
      //});



      return Ok();
    }


  }


}




