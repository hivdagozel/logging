using Elastic.Clients.Elasticsearch;
using LogConsumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    string elasticsearchUrl =
        builder.Configuration["Elasticsearch:Url"]
        ?? "http://localhost:9200";

    var settings =
        new ElasticsearchClientSettings(
            new Uri(elasticsearchUrl));

    return new ElasticsearchClient(settings);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();