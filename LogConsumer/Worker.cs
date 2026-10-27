using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly List<IChannel> _channels = [];

    private IConnection? _connection;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        ElasticsearchClient elasticsearchClient)
    {
        _logger = logger;
        _configuration = configuration;
        _elasticsearchClient = elasticsearchClient;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName =
                _configuration["RabbitMq:HostName"] ?? "localhost",

            Port =
                int.TryParse(
                    _configuration["RabbitMq:Port"],
                    out int rabbitMqPort)
                    ? rabbitMqPort
                    : 5672,

            UserName =
                _configuration["RabbitMq:UserName"] ?? "guest",

            Password =
                _configuration["RabbitMq:Password"] ?? "guest",

            AutomaticRecoveryEnabled = true,

            ClientProvidedName =
                "aspnetlearning-log-consumer"
        };

        _connection = await factory.CreateConnectionAsync(
            cancellationToken: stoppingToken);

        await StartConsumerAsync(
            GetRequiredSetting("Queues:ServiceA:Name"),
            GetRequiredSetting("Queues:ServiceA:Index"),
            stoppingToken);

        await StartConsumerAsync(
            GetRequiredSetting("Queues:ServiceB:Name"),
            GetRequiredSetting("Queues:ServiceB:Index"),
            stoppingToken);

        await StartConsumerAsync(
            GetRequiredSetting("Queues:Common:Name"),
            GetRequiredSetting("Queues:Common:Index"),
            stoppingToken);

        _logger.LogInformation(
            "LogConsumer RabbitMQ kuyruklarını dinliyor.");

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "LogConsumer durduruluyor.");
        }
        finally
        {
            foreach (IChannel channel in _channels)
            {
                await channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
    }

    private async Task StartConsumerAsync(
        string queueName,
        string indexName,
        CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException(
                "RabbitMQ bağlantısı oluşturulmadı.");
        }

        IChannel channel =
            await _connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        _channels.Add(channel);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        var consumer =
            new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            byte[] body =
                eventArgs.Body.ToArray();

            string json =
                Encoding.UTF8.GetString(body);

            try
            {
                LogDocument? logDocument =
                    JsonSerializer.Deserialize<LogDocument>(
                        json,
                        _jsonOptions);

                if (logDocument is null)
                {
                    throw new InvalidOperationException(
                        "Log mesajı JSON nesnesine dönüştürülemedi.");
                }

                var response =
                    await _elasticsearchClient.IndexAsync(
                        logDocument,
                        descriptor =>
                            descriptor.Index(indexName),
                        cancellationToken);

                if (!response.IsValidResponse)
                {
                    throw new InvalidOperationException(
                        "Log Elasticsearch'e kaydedilemedi.");
                }

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Log Elasticsearch'e yazıldı. Queue: {QueueName}, Index: {IndexName}",
                    queueName,
                    indexName);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Log işlenemedi. Queue: {QueueName}",
                    queueName);

                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Kuyruk dinlenmeye başladı. Queue: {QueueName}, Index: {IndexName}",
            queueName,
            indexName);
    }

    private string GetRequiredSetting(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException(
                $"Ayar bulunamadı: {key}");
    }
}