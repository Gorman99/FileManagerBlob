using Azure.Storage.Blobs;
using FileManagerBlob.Api.Services.Interfaces;
using FileManagerBlob.Api.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton( x => new BlobServiceClient( builder.Configuration.GetValue<string>("BlobConnectionString")));
builder.Services.AddSingleton<IFileMangerService,FileMangerService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();