namespace SampleAPI.Services
{
  public record AsyncResponse(string response);


  public interface IAsyncService
  {
    Task<AsyncResponse> GetAsync(CancellationToken cancellationToken);
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
      return Task.FromResult(new AsyncResponse("OK"));
    }
  }
}
