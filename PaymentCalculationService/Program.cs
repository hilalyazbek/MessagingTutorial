using System;
using System.Threading.Tasks;
using NServiceBus;
using Shared.Commands;
using Shared.Events;

class Program
{
    static async Task Main()
    {
        Console.Title = "Payment Calculation Service";

        var endpointConfiguration = new EndpointConfiguration("PaymentCalculationService");
        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString("host=localhost;port=5672;username=guest;password=guest");
        transport.UseConventionalRoutingTopology(QueueType.Classic);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UsePersistence<LearningPersistence>();

        try
        {
            var endpointInstance = await Endpoint.Start(endpointConfiguration);
            Console.WriteLine("Payment Calculation Service started successfully!");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting endpoint: {ex.Message}");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}

public class AddItemToCartHandler : IHandleMessages<AddItemToCartCommand>
{
    public Task Handle(AddItemToCartCommand message, IMessageHandlerContext context)
    {
        try
        {
            Console.WriteLine($"Received AddItemToCartCommand for product: {message.ProductName}");
            Console.WriteLine($"Calculating payment for cart: {message.CartId}");
            Console.WriteLine($"Total amount: {message.Price * message.Quantity:C}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing command: {ex.Message}");
            throw;
        }
    }
}

public class CartUpdatedHandler : IHandleMessages<CartUpdatedEvent>
{
    public Task Handle(CartUpdatedEvent message, IMessageHandlerContext context)
    {
        try
        {
            Console.WriteLine($"Received CartUpdatedEvent for cart: {message.CartId}");
            Console.WriteLine($"Total amount: {message.TotalAmount:C}");
            Console.WriteLine($"Total items: {message.TotalItems}");
            Console.WriteLine($"Updated at: {message.UpdatedAt}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing event: {ex.Message}");
            throw;
        }
    }
}
