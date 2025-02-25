using SampleAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Scoped servislerin Instance almasý için AddScoped kullanýlýr.
// Scoped servisler request bazlý çalýþýr ayný request içindeki ayný servisler ayný instance deðerleri sahiptir. 
// AsyncService request bazlý tekillik saðlar.
builder.Services.AddScoped<IAsyncService, AsyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}


// middleware

app.Use(async (context, next) =>
{
  await Console.Out.WriteLineAsync("Main Request Thread " + Thread.CurrentThread.ManagedThreadId);
  await next();

  await Console.Out.WriteLineAsync("HTTP STATUS" + context.Response.StatusCode);

});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
