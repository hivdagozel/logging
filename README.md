# Logging

Bu proje, iki ASP.NET Core servisinin loglarını Serilog ve RabbitMQ kullanarak merkezi bir LogConsumer uygulamasına aktarır. LogConsumer kayıtları Elasticsearch indekslerine yazar ve kayıtlar Kibana üzerinden görüntülenebilir.

## Mimari

```text
ServiceA ─┐
          ├─> RabbitMQ topic exchange ─> LogConsumer ─> Elasticsearch ─> Kibana
ServiceB ─┘
```

Her servis için ayrı bir log indeksi ve tüm servisler için ortak bir indeks bulunur:

- `project-servicea-logs`
- `project-serviceb-logs`
- `project-microservices-logs`

## Kullanılan teknolojiler

- .NET 10
- ASP.NET Core Web API
- Serilog
- RabbitMQ
- Elasticsearch
- Kibana
- Docker Compose

## Projeler

- `ServiceA`: `servicea` routing key değeriyle log üretir.
- `ServiceB`: `serviceb` routing key değeriyle log üretir.
- `LogConsumer`: RabbitMQ kuyruklarını dinler ve logları Elasticsearch indekslerine yazar.

## Altyapıyı başlatma

Docker Desktop açıkken depo klasöründe şu komut çalıştırılır:

```powershell
docker compose up -d
```

Servis adresleri:

- RabbitMQ: http://localhost:15672
- Elasticsearch: http://localhost:9200
- Kibana: http://localhost:5601

RabbitMQ kullanıcı adı ve parolası `guest` değeridir.

## RabbitMQ yapılandırması

`project.logs` adında `topic` türünde bir exchange oluşturulur.

Kuyruklar ve binding değerleri:

| Kuyruk | Routing key |
|---|---|
| `servicea-logs-queue` | `servicea` |
| `serviceb-logs-queue` | `serviceb` |
| `microservices-logs-queue` | `servicea` |
| `microservices-logs-queue` | `serviceb` |

## Çalıştırma

Önce `LogConsumer`, ardından `ServiceA` ve `ServiceB` çalıştırılır. Servislerin `.http` dosyalarındaki Information, Warning ve Error istekleri gönderilerek örnek loglar oluşturulur.

Elasticsearch indeksleri şu adres üzerinden kontrol edilebilir:

```text
http://localhost:9200/_cat/indices?v
```

Kibana içerisinde indeks adları kullanılarak üç ayrı Data View oluşturulabilir.
