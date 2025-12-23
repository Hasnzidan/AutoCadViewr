var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System
            .Text
            .Json
            .Serialization
            .ReferenceHandler
            .IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 3024;
        options.JsonSerializerOptions.NumberHandling = System
            .Text
            .Json
            .Serialization
            .JsonNumberHandling
            .AllowNamedFloatingPointLiterals;
    });

builder.Services.AddHttpClient();
builder.Services.AddScoped<DWGViewerAPI.Infrastructure.FileDownloader>();
builder.Services.AddScoped<DWGViewerAPI.Services.ColorResolver>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IBoundaryLoopService,
    DWGViewerAPI.Services.BoundaryLoopService
>();

builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.LineConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.CircleConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.TextConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.InsertConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.HatchConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.RegionConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.MLineConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.SolidConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.LeaderConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.ViewportConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.DimensionConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.EllipseConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.PolylineConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.SplineConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityTypeConverter,
    DWGViewerAPI.Services.Converters.PointConverter
>();

builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IEntityConverter,
    DWGViewerAPI.Services.EntityConverter
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IDwgReaderService,
    DWGViewerAPI.Services.DwgReaderService
>();
builder.Services.AddScoped<
    DWGViewerAPI.Services.Interfaces.IDwgParserService,
    DWGViewerAPI.Services.DwgParserService
>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();
app.Run();
