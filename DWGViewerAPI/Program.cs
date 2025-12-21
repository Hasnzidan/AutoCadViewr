var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Register Custom Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<DWGViewerAPI.Infrastructure.FileDownloader>();
builder.Services.AddScoped<DWGViewerAPI.Services.ColorResolver>();

// Register Entity Type Converters (Strategy Pattern)
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.LineConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.CircleConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.ArcConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.LwPolylineConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.TextConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.InsertConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.HatchConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityTypeConverter, DWGViewerAPI.Services.Converters.MLineConverter>();

builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IEntityConverter, DWGViewerAPI.Services.EntityConverter>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IDwgReaderService, DWGViewerAPI.Services.DwgReaderService>();
builder.Services.AddScoped<DWGViewerAPI.Services.Interfaces.IDwgParserService, DWGViewerAPI.Services.DwgParserService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();
app.Run();