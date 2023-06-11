# Using Azure Service Bus

::: tip
Wolverine is only supporting Azure Service Bus queues for right now, but support for publishing
or subscribing through [Azure Service Bus topics and subscriptions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions) is planned.
:::

Wolverine supports [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) as a messaging transport through the WolverineFx.AzureServiceBus nuget.

## Connecting to the Broker

After referencing the Nuget package, the next step to using Azure Service Bus within your Wolverine
application is to connect to the service broker using the `UseAzureServiceBus()` extension
method as shown below in this basic usage:

<!-- snippet: sample_basic_connection_to_azure_service_bus -->
<a id='snippet-sample_basic_connection_to_azure_service_bus'></a>
```cs
using var host = await Host.CreateDefaultBuilder()
    .UseWolverine((context, opts) =>
    {
        // One way or another, you're probably pulling the Azure Service Bus
        // connection string out of configuration
        var azureServiceBusConnectionString = context
            .Configuration
            .GetConnectionString("azure-service-bus");

        // Connect to the broker in the simplest possible way
        opts.UseAzureServiceBus(azureServiceBusConnectionString)

            // Let Wolverine try to initialize any missing queues
            // on the first usage at runtime
            .AutoProvision()

            // Direct Wolverine to purge all queues on application startup.
            // This is probably only helpful for testing
            .AutoPurgeOnStartup();

        // Or if you need some further specification...
        opts.UseAzureServiceBus(azureServiceBusConnectionString,
            azure => { azure.RetryOptions.Mode = ServiceBusRetryMode.Exponential; });
    }).StartAsync();
```
<sup><a href='https://github.com/JasperFx/wolverine/blob/main/src/Transports/Wolverine.AzureServiceBus.Tests/DocumentationSamples.cs#L15-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_basic_connection_to_azure_service_bus' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The advanced configuration for the broker is the [ServiceBusClientOptions](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusclientoptions?view=azure-dotnet) class from the Azure.Messaging.ServiceBus
library. 

For security purposes, there are overloads of `UseAzureServiceBus()` that will also accept and opt into Azure Service Bus authentication with:

1. [TokenCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.core.tokencredential?view=azure-dotnet)
2. [AzureNamedKeyCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.azurenamedkeycredential?view=azure-dotnet)
3. [AzureSasCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.azuresascredential?view=azure-dotnet)







