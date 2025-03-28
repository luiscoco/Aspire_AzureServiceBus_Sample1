using Aspire.Hosting.Azure;
using Azure.Provisioning.ServiceBus;

var builder = DistributedApplication.CreateBuilder(args);

//var serviceBus = builder.AddAzureServiceBus("servicebus");


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

