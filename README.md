# Building a Distributed System with NServiceBus and RabbitMQ in .NET

### Introduction
**NServiceBus** is a powerful messaging framework for .NET that enables building distributed systems with ease. When combined with **RabbitMQ**, it provides a robust foundation for implementing message-based communication between services.

In this blog post, I'll guide you through setting up a distributed system using NServiceBus and RabbitMQ. We'll create two services: a Cart Service and a Payment Calculation Service, demonstrating commands, events, and message handling.

#### Setup
To set up our distributed system, ensure you have the following prerequisites:
- .NET Core SDK
- Docker Desktop

##### Installing RabbitMQ
First, let's set up RabbitMQ using Docker. Open a terminal window and enter the following command:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest rabbitmq:3-management
```

This command:
- Pulls the official RabbitMQ image with management plugin
- Creates a container named "rabbitmq"
- Exposes port 5672 for AMQP protocol
- Exposes port 15672 for the management interface
- Sets default credentials (username: guest, password: guest)

To verify that everything is running correctly, access the RabbitMQ management interface by visiting [http://localhost:15672][1]. Log in using:
- Username: guest
- Password: guest

![RabbitMQ][2]

##### Project Structure
We'll create three projects:
1. `CartService` - A console application that manages the shopping cart
2. `PaymentCalculationService` - A console application that calculates payments
3. `Shared` - A class library containing shared message contracts

I chose console applications for simplicity, but this can be any other type of project.

Create the solution and projects using these commands:

```bash
dotnet new sln -n MessagingTutorial
dotnet new console -n CartService
dotnet new console -n PaymentCalculationService
dotnet new classlib -n Shared
dotnet sln add CartService/CartService.csproj PaymentCalculationService/PaymentCalculationService.csproj Shared/Shared.csproj
```

##### Required NuGet Packages
Add the following packages to both service projects:

```bash
# Core NServiceBus packages
dotnet add package NServiceBus
dotnet add package NServiceBus.RabbitMQ

# Add reference to shared project
dotnet add reference ../Shared/Shared.csproj
```

##### Message Contracts
In the Shared project, we define our message contracts. Message contracts are the data structures that define the shape and content of messages exchanged between services. They serve as a contract between the sender and receiver, ensuring both parties understand the data being transmitted. In NServiceBus, we typically use two types of messages:

1. **Commands**: Represent actions that should be performed by a specific service. They are sent to a particular endpoint and should be named in the imperative form (e.g., AddItemToCart, ProcessPayment).
2. **Events**: Represent something that has happened in the system. They are published and can be subscribed to by multiple services. Events should be named in the past tense (e.g., ItemAddedToCart, PaymentProcessed).

Here are our message contracts:
> Notice the marker interfaces ICommand and IEvent. Those are used to let NServiceBus know which one is which.

1. Command (AddItemToCartCommand.cs):
```csharp
public class AddItemToCartCommand : ICommand
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
```

2. Event (CartUpdatedEvent.cs):
```csharp
public class CartUpdatedEvent : IEvent
{
    public Guid CartId { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

##### Configuring NServiceBus
Both services need to be configured as NServiceBus endpoints. The configuration defines how the endpoint behaves, including transport, serialization, and persistence settings. Let's break down each part:

```csharp
// This name is used to identify the endpoint in the system
var endpointConfiguration = new EndpointConfiguration("ServiceName");

// This tells NServiceBus how to send and receive messages
var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();

// The connection string includes the host, port, and credentials
// In production, you should use secure credentials and consider using configuration files
transport.ConnectionString("host=localhost;port=5672;username=guest;password=guest");

// This automatically sets up routing based on message types and endpoints
transport.UseConventionalRoutingTopology(QueueType.Classic);

// NServiceBus needs to know how to convert messages to/from bytes
endpointConfiguration.UseSerialization<SystemJsonSerializer>();

// This automatically creates the necessary queues in RabbitMQ
endpointConfiguration.EnableInstallers();

// NServiceBus needs to store some data (like subscription information)
// LearningPersistence is for development; you can use other forms of Persistence
endpointConfiguration.UsePersistence<LearningPersistence>();
```

##### Implementing Message Handlers
In the PaymentCalculationService, we implement handlers for both commands and events:

```csharp
public class AddItemToCartHandler : IHandleMessages<AddItemToCartCommand>
{
    public Task Handle(AddItemToCartCommand message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Received AddItemToCartCommand for product: {message.ProductName}");
        Console.WriteLine($"Calculating payment for cart: {message.CartId}");
        Console.WriteLine($"Total amount: {message.Price * message.Quantity:C}");
        return Task.CompletedTask;
    }
}

public class CartUpdatedHandler : IHandleMessages<CartUpdatedEvent>
{
    public Task Handle(CartUpdatedEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Received CartUpdatedEvent for cart: {message.CartId}");
        Console.WriteLine($"Total amount: {message.TotalAmount:C}");
        Console.WriteLine($"Total items: {message.TotalItems}");
        Console.WriteLine($"Updated at: {message.UpdatedAt}");
        return Task.CompletedTask;
    }
}
```

##### Sending Messages
In the CartService, we send commands and publish events:

```csharp
// Send command
var command = new AddItemToCartCommand
{
    CartId = Guid.NewGuid(),
    ProductId = Guid.NewGuid(),
    ProductName = "Sample Product",
    Price = 99.99m,
    Quantity = 1
};

await endpointInstance.Send("PaymentCalculationService", command);

// Publish event
var cartEvent = new CartUpdatedEvent
{
    CartId = command.CartId,
    TotalAmount = command.Price * command.Quantity,
    TotalItems = command.Quantity,
    UpdatedAt = DateTime.UtcNow
};

await endpointInstance.Publish(cartEvent);
```

#### Running the Application
1. Make sure the RabbitMQ container is running:
```bash
docker ps | findstr rabbitmq
```
![running docker container][3]

2. Run both services in separate console windows:
```bash
# Terminal 1
cd CartService
dotnet run

# Terminal 2
cd PaymentCalculationService
dotnet run
```
![run services][4]

3. In the CartService, press 'A' to add an item to the cart
4. Watch the PaymentCalculationService receive and process the command and event

![Messaging solution][5]

Let's check out the Queues. I stopped the PaymentCalculationService and sent multiple commands. Notice how the Queue has messages now that are waiting to be processed once the PaymentCalculationService is back up.

![Messages in the Queues][6]

I will stop the CartService now to make sure that it is not sending any new messages and start the PaymentCalculationService. Notice how the messages will be proccessed once the service is back up.

![Processed pending messages][7]

#### SeeSharp
There you have it! We've successfully set up a distributed system using NServiceBus and RabbitMQ. I've demonstrated how to send commands between services, publish events, and handle messages. This pattern can be extended to build more complex distributed systems with multiple services.

If you enjoyed this content, follow me on [Medium][8] and [LinkedIn][9].


[1]: http://localhost:15672
[2]: //images.ctfassets.net/3cmq6jppygp8/7576shF7tPROAMrqSKH4nS/6f7ea9191c22b97858f85ff8322ac19f/rabbitmq.png
[3]: //images.ctfassets.net/3cmq6jppygp8/2dOcXlMz4SJjUTFY2bTgRU/82788924cee470f5ec16430000a0243f/runningdockercontainer.png
[4]: //images.ctfassets.net/3cmq6jppygp8/1lXFopqpJFNBmvrMxL3bZ6/cf5d511fe723e66730589d65d556794f/run_services.png
[5]: //images.ctfassets.net/3cmq6jppygp8/jQ4DH2AXfuxb0cNYCnV4D/c29180b53b4b58cef1fa097be44f6861/working_solution.png
[6]: //images.ctfassets.net/3cmq6jppygp8/7Ehrf3ahmrscw1vwR8C24B/dbc1db0643d223915b93990e9dd41be5/Queues_created.png
[7]: //images.ctfassets.net/3cmq6jppygp8/7DJSYAkV2ZSCUriy6bFVcs/d20291d31e4b77a055f11e0092438f51/processed_pending_messages.png
[8]: https://medium.com/@hilalyazbek
[9]: https://www.linkedin.com/in/hilalyazbek