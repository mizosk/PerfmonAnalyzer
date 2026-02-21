using PerfmonAnalyzer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// アプリケーションサービスの登録
builder.Services.AddScoped<ICsvImporter, CsvImporter>();
builder.Services.AddSingleton<ISlopeAnalyzer, SlopeAnalyzer>();
builder.Services.AddSingleton<IDataService, InMemoryDataService>();
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();

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

// CORS を有効化（開発環境のみ）
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowReactDev");
}

// 静的ファイルの配信を有効化（本番環境用）
// wwwroot フォルダ内のファイル（HTML, CSS, JS等）をWebで公開
app.UseDefaultFiles(); // index.html を自動で返す
app.UseStaticFiles();  // 静的ファイルを配信

app.UseAuthorization();

app.MapControllers();

// SPA（React）のフォールバック設定
// どのURLにアクセスしても、APIでなければindex.htmlを返す
// これにより、Reactのルーティングが正しく動作する
app.MapFallbackToFile("index.html");

app.Run();

// WebApplicationFactory でテストからアクセスするための部分クラス
public partial class Program { }
