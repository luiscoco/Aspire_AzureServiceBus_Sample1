# AIntegrating .NET Aspire 9.1 with Azure ServiceBus

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



## 3. We load the Nuget packages in the AppHost project



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

var serviceBus = builder.AddAzureServiceBus("servicebus");

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

In more detail:

a) We add an Azure Service Bus resource to the application with the name servicebus.

```csharp
var serviceBus = builder.AddAzureServiceBus("servicebus");
```

b) Adds a queue named queue1 with the Aspire logical name queueOne.

Disables dead lettering when messages expire.

```csharp
var queue = serviceBus.AddServiceBusQueue("queueOne", "queue1")
    .WithProperties(queue => queue.DeadLetteringOnMessageExpiration = false);
```

c) 

```csharp

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
