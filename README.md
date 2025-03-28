# Integrating .NET Aspire 9.1 with Azure ServiceBus

In this post, we’ll walk through how to create an Azure ServiceBus using .NET Aspire 9.1

You’ll learn how to define and provision these resources as part of a distributed application

For more information about this post, please visit the official website: 

https://learn.microsoft.com/es-es/dotnet/aspire/messaging/azure-service-bus-integration?tabs=dotnet-cli

## 1. Prerrequisites

### 1.1. Install .NET 9

Visit this web site (https://dotnet.microsoft.com/es-es/download/dotnet/9.0) and download **Windows x64 SDK 9.0.202**

![image](https://github.com/user-attachments/assets/87e72641-7c88-4839-9bdb-91f64568c20a)

### 1.2. Install Visual Studio 2022 v17.3 Community Edition

https://visualstudio.microsoft.com/downloads/

![image](https://github.com/user-attachments/assets/653307c3-fe36-43c0-ac29-505d4dead3dd)

### 1.3. Run these commands to configure Azure 

We first log in Azure

```
az login
```

![image](https://github.com/user-attachments/assets/ff2e6b77-1656-47a9-a56f-d337d8063ffd)

![image](https://github.com/user-attachments/assets/53bc1554-751c-4699-8d43-04c2683f01f6)

We verify Azure account information

```
az account show
```

![image](https://github.com/user-attachments/assets/054f9148-3b93-4563-8dd5-72c34f25a5d2)

## 2. Create a new .NET Aspire Empty App

We run **Vistual Studio 2022** and create a new project

We select the project template for **.NET Aspire Empty Application**

![image](https://github.com/user-attachments/assets/27487f4a-fd85-43cf-92aa-da548b6d0a6e)

We input the project name and location

We select the .NET 9 or .NET 10 framework and press on the Create button

This is the solution structure

![image](https://github.com/user-attachments/assets/ef5b7f61-c200-4b2d-9861-22a69534658d)

## 3. We load the Nuget packages in the AppHost project

![image](https://github.com/user-attachments/assets/3879fbce-a19b-48b9-b9f7-fdd959937f61)

## 4. AppHost project source code

This C# code defines a **distributed application** using .NET Aspire, specifically targeting Azure services with infrastructure provisioning (Azure ServiceBus) support

This code is ideal for **cloud-native messaging setups** using Azure and .NET Aspire 

It sets up a **Azure Service Bus** infrastructure using Aspire:

a) Adds a queue and a topic with a subscription.

b) Configures message filters and retry logic.

c) Connects a background worker project that consumes those resources.

**Program.cs**

```csharp
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("service-bus")
    .ConfigureInfrastructure(infra =>
    {
        var serviceBusNamespace = infra.GetProvisionableResources()
                                       .OfType<ServiceBusNamespace>()
                                       .Single();

        serviceBusNamespace.Name = "luiscocoservicebus";

        //serviceBusNamespace.Sku = new ServiceBusSku
        //{
        //    Tier = ServiceBusSkuTier.Premium
        //};
        //serviceBusNamespace.Tags.Add("ExampleKey", "Example value");
    });

var queue = serviceBus.AddServiceBusQueue("queueOne", "queue1")
    .WithProperties(queue => queue.DeadLetteringOnMessageExpiration = false);

var subscription = serviceBus.AddServiceBusTopic("topicOne", "topic1")
    .AddServiceBusSubscription("sub1")
    .WithProperties(subscription =>
    {
        subscription.MaxDeliveryCount = 10;

        var rule = new AzureServiceBusRule("app-prop-filter-1")
        {
            CorrelationFilter = new()
            {
                ContentType = "application/text",
                CorrelationId = "id1",
                Subject = "subject1",
                MessageId = "msgid1",
                ReplyTo = "someQueue",
                ReplyToSessionId = "sessionId",
                SessionId = "session1",
                SendTo = "xyz"
            }
        };
        subscription.Rules.Add(rule);
    });

builder.AddProject<Projects.ServiceBusWorker>("worker")
    .WithReference(queue).WaitFor(queue)
    .WithReference(subscription).WaitFor(subscription);

builder.Build().Run();
```

We also have to set the **AppHost project secrets**

We right click on the AppHost project name and select the menu option **Manage User Secrets**

![image](https://github.com/user-attachments/assets/5499aa75-1d18-4d77-bc18-01d64522a685)

We input the secrets in the **secrets.json** file:

```
{
  "Azure": {
    "SubscriptionId": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "AllowResourceGroupCreation": true,
    "ResourceGroup": "luispruebamyRG",
    "Location": "westeurope",
    "CredentialSource": "Default",
    "Tenant": "luiscocoenriquezhotmail.onmicrosoft.com"
  }
}
```

## 5. We create a C# console project (ServiceBusWorker)

![image](https://github.com/user-attachments/assets/a2f4a800-d1cc-4162-bb6c-b03f3f22a7ee)

## 6. We load the Nuget packages in C# console project

![image](https://github.com/user-attachments/assets/9773cde8-d28b-449f-8ddf-2083d74be20a)

We add the **.NET Aspire Orchestrator Support** in the Console application

![image](https://github.com/user-attachments/assets/c04051b7-fd36-4744-87a9-21e0a880d7f2)

We confirm the Console project was added as reference in the AppHost project

![image](https://github.com/user-attachments/assets/2847a9bf-22fa-45e9-ab66-19241154f5bf)

We also has to add the **ServiceDefaults** project as reference in the Console project

![image](https://github.com/user-attachments/assets/80bed940-5bcd-4d49-8ca7-86afbfe6bcc1)

## 7. We configure the appsettings.json file in the ServiceWorker project

```json
"ConnectionStrings": {
"serviceBusConnectionName": "luiscocoservicebus.servicebus.windows.net"
}
```

## 8. We Add the Consumer and Producer in the ServiceWorker project

See the **ServiceWorker** project structure

The **Producer** is a background worker in .NET that periodically **sends messages to an Azure Service Bus** queue or topic using the ServiceBusSender

This **worker** could simulate a **message-producing microservice** — like a telemetry sender, event generator, or job dispatcher — in a **distributed system architecture** using **Azure Service Bus**

This is **Producer** class code:

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceBusWorker;

internal sealed class Producer(ServiceBusSender sender, ILogger<Producer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting producer...");

        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!cancellationToken.IsCancellationRequested)
        {
            await periodicTimer.WaitForNextTickAsync(cancellationToken);

            await sender.SendMessageAsync(new ServiceBusMessage($"Hello, World! It's {DateTime.Now} here."), cancellationToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping producer...");
        return Task.CompletedTask;
    }
}
```

The **Consumer** is a background worker in .NET that **consumes messages from an Azure Service Bus** queue or topic using the ServiceBusProcessor

This **worker** acts as a message consumer in a distributed system, listening to Azure Service Bus for events or tasks to process — e.g., order handling, event logging, or notifications.

This is **Consumer** class code:

```csharp
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceBusWorker;

internal sealed class Consumer(ServiceBusProcessor processor, ILogger<Consumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        processor.ProcessMessageAsync += MessageHandler;

        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private Task MessageHandler(ProcessMessageEventArgs args)
    {
        // Process the message
        logger.LogInformation("Received message: {Message}", Encoding.UTF8.GetString(args.Message.Body));

        return Task.CompletedTask;
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping consumer...");
        return Task.CompletedTask;
    }
}
```

## 9. We run the application and see the results

We set **AppHost** project as the **StartUp project**

![image](https://github.com/user-attachments/assets/3c55577a-1b80-499e-9685-55fc969cfe40)

We run the application and automatically we are redirected to the Aspire Dashboard

![image](https://github.com/user-attachments/assets/b89055b4-d631-4e02-8413-b87c7f5670fc)

We can verify in the **Worker** outputs

![image](https://github.com/user-attachments/assets/115a3201-1ac4-473c-aa45-0c017b8a50b5)

We also can login in Azure Portal and search for the created ServiceBus

![image](https://github.com/user-attachments/assets/63dae68a-b6d3-40a6-91b7-7f6aa439d609)

We verify the queue and topic created

![image](https://github.com/user-attachments/assets/31567467-ed85-4a4f-ae38-fb6330fe23b5)

![image](https://github.com/user-attachments/assets/7eed9b09-47d7-4209-bebd-2e29e8b63184)




