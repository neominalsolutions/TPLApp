using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SampleAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class PLINQController : ControllerBase
  {

    [HttpGet("paralelFor")]
    public IActionResult asParalel(CancellationToken token)
    {

      try
      {
        var sp = Stopwatch.StartNew();
        // v1
        var query = from item in Enumerable.Range(0, 100).AsParallel().AsOrdered().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithCancellation(token)
                    where item % 2 == 0
                    select item;
        sp.Stop();
        Console.Out.WriteLine("sp " + sp.ElapsedMilliseconds);


        // Kod burada artık senkron döner. veri işlem işlemleri tek bir thread de gerçekleşir. 
        query.ToList().ForEach(async (item) =>
        {
          Console.Out.WriteLine("Foreach" + item);
        });

        // Not: Genel doğru kod akışı
        // Lambda expression LINQ
        var sp1 = Stopwatch.StartNew();
       var query2 = Enumerable.Range(0, 100).AsParallel().Where(x => x % 0 == 0).WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithCancellation(token);
        sp1.Stop();
        Console.Out.WriteLine("sp1 " + sp.ElapsedMilliseconds);

        ConcurrentBag<int> bg = new();

        // Multi Thread 
        query2.ForAll((item) =>
        {
          bg.Add(item);
          Console.Out.WriteLine(item);
          Console.Out.WriteLine("Forall Thread Id " + Thread.CurrentThread.ManagedThreadId);
        });

        // ramde sıralı hale getirirz.
        var ordered =  bg.OrderBy(x => x);

      }
      catch (AggregateException ex) // birden fazla hata durumu varsa
      {
        foreach (var item in ex.InnerExceptions)
        {
          Console.Out.WriteLine(item.Message);
        }
      }

      return Ok();
    }


  }
}
