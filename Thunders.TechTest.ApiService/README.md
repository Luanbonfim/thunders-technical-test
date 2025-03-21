# Toll Usage API Service

Este serviço é responsável por gerenciar os dados de utilização das praças de pedágio e gerar relatórios conforme solicitado.

## Requisitos

- .NET 8.0 ou superior
- SQL Server
- RabbitMQ

## Configuração

1. Configure as variáveis de ambiente necessárias:
   - `ConnectionStrings__ThundersTechTestDb`: String de conexão com o banco de dados SQL Server
   - `ConnectionStrings__RabbitMq`: URL de conexão com o RabbitMQ

2. Configure as features no arquivo `appsettings.json`:
   ```json
   {
     "Features": {
       "UseMessageBroker": true,
       "UseEntityFramework": true
     },
     "QueueSettings": {
       "TollUsageQueueName": "toll-usage-queue"
     },
     "EnvironmentVariables": {
       "DatabaseConnection": "ConnectionStrings__ThundersTechTestDb",
       "RabbitMqConnection": "ConnectionStrings__RabbitMq"
     }
   }
   ```

## Endpoints

### POST /api/v1.0/toll-usage/create-toll-usages
Recebe dados de utilização de pedágio.

#### Request Body:
```json
[
   {
   "usageDateTime": "2024-03-20T10:00:00Z",
   "tollBooth": "Praça 1",
   "city": "São Paulo",
   "state": "SP",
   "amount": 10.50,
   "vehicleType": "Car"
   }
]
```

#### Respostas:
- 200 OK: Dados recebidos com sucesso
  ```json
  {
    "isSuccess": true,
    "data": "Toll Usages Successfully created",
    "message": null
  }
  ```
- 400 Bad Request: Erro ao processar os dados
  ```json
  {
    "isSuccess": false,
    "data": null,
    "message": "Error creating toll usage"
  }
  ```

### POST /api/v1.0/toll-usage/generate-report
Aciona a geração de relatórios. O serviço valida se o tipo de relatório é válido antes de iniciar a geração.

#### Tipos de Relatórios Disponíveis:
- `HourlyByCityReport`: Relatório de valores totais por hora por cidade
- `TopTollboothsReport`: Relatório das praças que mais faturaram
- `VehicleTypesByTollboothReport`: Relatório de tipos de veículos por praça

#### Request Body:
```json
{
  "startDate": "2024-03-01T00:00:00Z",
  "endDate": "2024-03-31T23:59:59Z",
  "reportType": "HourlyByCityReport",
  "parameters": {
    // Parâmetros específicos para cada tipo de relatório
    // TopTollboothsReport: "tollboothsAmount": 10
    // VehicleTypesByTollboothReport: "tollBoothId": "guid-do-pedagio"
  }
}
```

#### Parâmetros por Tipo de Relatório:
- `HourlyByCityReport`: Não requer parâmetros adicionais
- `TopTollboothsReport`: 
  - `tollboothsAmount`: Número inteiro positivo que indica quantas praças devem ser retornadas
- `VehicleTypesByTollboothReport`:
  - `tollBoothId`: GUID da praça de pedágio

#### Respostas:
- 200 OK: Relatório gerado com sucesso
  ```json
  {
    "isSuccess": true,
    "data": "Report Generation Successfully Triggered",
    "message": null
  }
  ```
- 400 Bad Request: Tipo de relatório inválido ou parâmetros inválidos
  ```json
  {
    "isSuccess": false,
    "data": null,
    "message": "StartDate is required"
  }
  ```
## Arquitetura

O serviço utiliza uma arquitetura baseada em mensageria para processamento assíncrono dos dados:

1. Recebimento de dados via API
2. Envio de mensagem para o broker (RabbitMQ)
3. Processamento assíncrono da mensagem
4. Persistência no banco de dados
5. Geração de relatórios sob demanda
