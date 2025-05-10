using System;
using NServiceBus;

namespace Shared.Events
{
    public class CartUpdatedEvent : IEvent
    {
        public Guid CartId { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 