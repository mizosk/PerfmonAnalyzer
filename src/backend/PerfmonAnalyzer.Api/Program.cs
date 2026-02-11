using PerfmonAnalyzer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// アプリケーションサービスの登録
builder.Services.AddScoped<ICsvImporter, CsvImporter>();
builder.Services.AddSingleton<ISlopeAnalyzer, SlopeAnalyzer>();
builder.Services.AddSingleton<IDataService, InMemoryDataService>();

// CORS設定 - React開発サーバー（http://localhost:5173）からのアクセスを許可
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger/OpenAPI設定
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

// CORS を有効化
app.UseCors("AllowReactDev");

app.UseAuthorization();

app.MapControllers();

app.Run();

// WebApplicationFactory でテストからアクセスするための部分クラス
public partial class Program { }
