using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetWebForms.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(200)]
        public string Email { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Order> Orders { get; set; }

        public Customer()
        {
            Orders = new HashSet<Order>();
            CreatedDate = DateTime.UtcNow;
        }
    }
}
