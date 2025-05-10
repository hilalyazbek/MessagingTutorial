using System;
using System.Threading.Tasks;
using NServiceBus;
using Shared.Commands;
using Shared.Events;

class Program
{
    static async Task Main()
    {
        Console.Title = "Cart Service";

        var endpointConfiguration = new EndpointConfiguration("CartService");
        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString("host=localhost;port=5672;username=guest;password=guest");
        transport.UseConventionalRoutingTopology(QueueType.Classic);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UsePersistence<LearningPersistence>();

        try
        {
            var endpointInstance = await Endpoint.Start(endpointConfiguration);
            Console.WriteLine("Cart Service started successfully!");
            Console.WriteLine("Press 'A' to add an item to cart");
            Console.WriteLine("Press 'Q' to quit");

            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.Q)
                    break;

                if (key.Key == ConsoleKey.A)
                {
                    try
                    {
                        var command = new AddItemToCartCommand
                        {
                            CartId = Guid.NewGuid(),
                            ProductId = Guid.NewGuid(),
                            ProductName = "Sample Product",
                            Price = 99.99m,
                            Quantity = 1
                        };
                        await endpointInstance.Send("PaymentCalculationService", command);
                        Console.WriteLine($"Sent AddItemToCartCommand for product: {command.ProductName}");

                        var cartEvent = new CartUpdatedEvent
                        {
                            CartId = command.CartId,
                            TotalAmount = command.Price * command.Quantity,
                            TotalItems = command.Quantity,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await endpointInstance.Publish(cartEvent);
                        Console.WriteLine($"Published CartUpdatedEvent for cart: {cartEvent.CartId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message: {ex.Message}");
                    }
                }
            }
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
