using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetWebForms.Models
{
    public enum OrderStatus
    {
        Pending,
        Shipped,
        Cancelled
    }

    public class Order
    {
        public int OrderId { get; set; }

        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        public virtual Customer Customer { get; set; }

        public DateTime OrderDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        public OrderStatus Status { get; set; }
    }
}
