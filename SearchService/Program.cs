using Elastic.Clients.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddLogging();

// Configure Elasticsearch client
var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("menu_items");

var client = new ElasticsearchClient(settings);
builder.Services.AddSingleton(client);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

