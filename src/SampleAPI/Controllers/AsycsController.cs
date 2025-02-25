using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleAPI.Services;
using System.Net;


namespace SampleAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AsycsController : ControllerBase
  {
    private readonly IAsyncService _asyncService;

    public AsycsController(IAsyncService asyncService)
    {
      _asyncService = asyncService;
    }


    [HttpGet("Sync")]
    public IActionResult GetSync()
    {
      System.Threading.Thread.Sleep(5000);
      Console.Out.WriteLine($"Sync {Thread.CurrentThread.ManagedThreadId}"); // Main Thread
      return Ok("Sync");
    }

    [HttpGet("Async")]
    public async Task<IActionResult> GetAsync()
    {
      // Task Run ile senkron bir kod bloğunun non blocking çalıştırmak için sarmalladık.
      var task = Task.Run(async () =>
      {
        await Task.Delay(5000); // 5 saniye sürecek olan db operasyonu, dosya okuma, http request vs.
        await Console.Out.WriteLineAsync($"Async Task {Thread.CurrentThread.ManagedThreadId}"); // Main Thread bloke edildimediğinde Main Thread'de bloke etmeden kullanabilir veya yepyeni bir thread'de çalışabilir.

      });


      return Ok("Async");
    }

    [HttpGet("taskFactory")]
    public async Task<IActionResult> GetTaskFactory()
    {


      // TaskFactory().StartNew ile senkron bir kod bloğunun non blocking çalıştırmak için sarmalladık.
      var task = new TaskFactory().StartNew((async () =>
      {
        await Task.Delay(5000); // 5 saniye sürecek olan db operasyonu, dosya okuma, http request vs.
        await Console.Out.WriteLineAsync($"Async Task {Thread.CurrentThread.ManagedThreadId}"); // Main Thread bloke edildimediğinde Main Thread'de bloke etmeden kullanabilir veya yepyeni bir thread'de çalışabilir.

        return Task.FromException(new Exception("Hata"));

      }));




      return Ok("Async");
    }

    [HttpGet("taskFactory2")]
    public async Task<IActionResult> Sample(CancellationToken token)
    {

      var tasks = new List<Task>();
      Random rnd = new Random();
      Object lockObj = new Object();
      String[] words6 = { "reason", "editor", "rioter", "rental",
                          "senior", "regain", "ordain", "rained" };


      foreach (var word6 in words6)
      {
        tasks.Add(Task.Factory.StartNew((word) =>
        {
          Console.Out.WriteLineAsync("Thread Id" + Thread.CurrentThread.ManagedThreadId);

          Char[] chars = word.ToString().ToCharArray();
          double[] order = new double[chars.Length];
          lock (lockObj) // Thread Safe çalışmak için Threadler arasında bir kilit mekanizması oluşturduk.
          {
            for (int ctr = 0; ctr < order.Length; ctr++)
              order[ctr] = rnd.NextDouble();
          }
          Array.Sort(order, chars);
          Console.WriteLine("{0} --> {1}", word,
                            new String(chars));
        }, word6));
      }


      foreach (Task item in tasks)
      {
        if (item.IsCanceled)
        {
          Console.Out.WriteLineAsync("Task Cancelled");
        }
        else if (item.IsFaulted)
        {
          Console.Out.WriteLineAsync("Task Faulted");
        }
        else if (item.IsCompleted)
        {
          Console.Out.WriteLineAsync("Task Completed");
        }

      }

      // Task.WaitAll ile tüm task'ların bitmesini bekleyebiliriz.
      Task.WaitAll(tasks.ToArray());

      return Ok();
    }


    [HttpGet("continueWith")]
    public async Task<IActionResult> ContinueWith()
    {

      HttpClient client = new HttpClient();
      // Db Insert
      var task = client.GetStringAsync("https://google.com").ContinueWith(async t =>
      {
        // asenkron çalışırken kod bloğu çalıuşma esnasında burdaki koda delegate edilir.

        var data = t.Result.Length; // t.Result ile önceki task'ın sonucuna erişebiliriz.

        await Console.Out.WriteLineAsync("Google: Task Completed" + data);

        if (t.IsFaulted)
        {
          await Console.Out.WriteLineAsync(t.Exception.Message);
        }
        else if (t.IsCompletedSuccessfully)
        {
          await Console.Out.WriteLineAsync("Task Completed");
        }
        else if (t.IsCanceled)
        {
          await Console.Out.WriteLineAsync("Task Cancelled");
        }

      });

      // API SEND DATA
      // ContinueWith Fetch yada axios daki Then ifades
      var task2 = client.GetStringAsync("https://neominal.com").ContinueWith(async t =>
      {
        // asenkron çalışırken kod bloğu çalıuşma esnasında burdaki koda delegate edilir.

        var data = t.Result.Length; // t.Result ile önceki task'ın sonucuna erişebiliriz.

        await Console.Out.WriteLineAsync("Neo: Task Completed" + data);

        if (t.IsFaulted)
        {
          await Console.Out.WriteLineAsync(t.Exception.Message);
        }
        else if (t.IsCompletedSuccessfully)
        {
          await Console.Out.WriteLineAsync("Task Completed");
        }
        else if (t.IsCanceled)
        {
          await Console.Out.WriteLineAsync("Task Cancelled");
        }

      });


      return Ok("OK");
    }



    [HttpGet("await")]
    public async Task<IActionResult> AsyncAwait()
    {

      HttpClient client = new HttpClient();
      // Db Insert
      // Await kodu bekletir ama bu bekletme blocking bir bekletme değil
      // non blocking bir bekletme yani sırada api farklı istekleri ele alabiliyor.
      // await kullanıyorsa 2 tane senaryomuz var ya alt işlem üst işlemin sonuç döndürmesini beklmektedir. yada sıralı çalışmayı garanti altına almak istemekteyiz.
      var task1 = await client.GetStringAsync("https://google.com");
      // await ile yazdığımızda verinin çözülmüş resolved olmuş halini alırız. artık task ile ilgili bir state takibi yapamayız.
      var data1 = task1.Length;
      await Console.Out.WriteLineAsync("Google: Task Completed" + data1);
      // API SEND DATA
      var task2 = await client.GetStringAsync("https://neominal.com");

      var data2 = task2.Length;
      await Console.Out.WriteLineAsync("Neo: Task Completed" + data2);

      // Blocklar. Result direk olarak await olmadan erişilince kod bloklanıyor.
      // var task = client.GetStringAsync("https://google.com").Result;

      //var task = client.GetStringAsync("https://google.com");
      //task.Wait();



      return Ok("OK");
    }


    [HttpGet("requestCancelation")]
    public async Task<IActionResult> RequestCancelation(CancellationToken cancellationToken)
    {

      try
      {
        // cancellationToken değeri asenkron kod bloğuna parametre olarak gönderilirse Task iptal süreci devreye girer.
        var task = Task.Run(() =>
        {
          Thread.Sleep(5000); // 100 milyon döngü

        }, cancellationToken);

        await task;

        if (cancellationToken.IsCancellationRequested)
        {
          await Console.Out.WriteLineAsync("Request Canceled");
          // request cancel edildiğinde exception fırlat.
          cancellationToken.ThrowIfCancellationRequested();
        }

        if (task.IsCompletedSuccessfully)
        {
          await Console.Out.WriteLineAsync("Task Completed");
        }
      }
      catch (OperationCanceledException ex)
      {
        Console.Out.WriteLineAsync("Task Canceled" + ex.Message);
        throw;
      }





      return Ok();
    }


    [HttpPost("requestCancelationCustomService")]

    public async Task<IActionResult> RequestCancelationWithCustom(CancellationToken cancellationToken)
    {

      var task = _asyncService.GetAsync(cancellationToken);

      if (task.IsCanceled)
      {
        await Console.Out.WriteLineAsync("Task Iptal edildi");
      }

      await task;

      return Ok(task.Result);


    }

  }
}
