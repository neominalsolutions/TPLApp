namespace SampleAPI.Services
{
  public record AsyncResponse(string response);


  public interface IAsyncService
  {
    Task<AsyncResponse> GetAsync(CancellationToken cancellationToken);
    Task<AsyncResponse> GetAsync();
  }

  public class AsyncService : IAsyncService
  {
    public Task<AsyncResponse> GetAsync(CancellationToken cancellationToken)
    {
      Thread.Sleep(5000);


      //
      if (cancellationToken.IsCancellationRequested)
      {
        // istek iptal edildiğinde Task iptal edilir. 
        return Task.FromCanceled<AsyncResponse>(cancellationToken);
      }

      // Başarı durumunda Task tamamlanır. Task.CompletedTask ile aynı işlevi görür.
      // Resolved Durumu
      return Task.FromResult(new AsyncResponse("OK"));
    }

    // Kendi servislerimizde bir hata durumu ortaya çıktığında biz bu hata durumlarını try catch blokları ile yakalayabiliyor muyuz ?
    public Task<AsyncResponse> GetAsync()
    {
      // Not: Kendi servisimz içindeki asenkron kod bloğunda bir hata meydana gelirse acaba bu hatayı catch bloğu yakalar mı
      var task2 = Task.Run(() => // Publish Event, SeriLog, SaveAsync
      {
        // Bu kod bloğu kodu catch düşürür mü ?
        throw new Exception("Hata");
      });

      // Not: Bu servis methodu içerisinde Task.FromException ve direkt olarak throw ile hata fırlatacak şekilde yazmak, hataları yakalamamızı sağlar.
      // Operayonun sonucunda işlemin bitmesini await ile yada wait ile beklemeden bir asenkron çağırı yaptık. Bu çağrı sonucunda asenkron çağrı içerisinde bir exception oluştu. bu Exception servisin ilk çağrıldığı ana kod bloğundan yakalayamayız. Mecbur olarak wait ile kodu bekletmek veya wait methodu ile senkron dönüştürmemiz gerkir. 

     //task2.Wait();

      return Task.FromResult(new AsyncResponse("OK"));


      // Rejected Durumu
      //return Task.FromException<AsyncResponse>(new NotImplementedException());
    }
  }
}
