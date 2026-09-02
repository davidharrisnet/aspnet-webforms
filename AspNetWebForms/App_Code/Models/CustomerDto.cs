using System;

namespace AspNetWebForms.Models
{
    // Flat projection returned by the Web API layer, deliberately excludes navigation
    // properties so it can be serialized safely after the owning DbContext is disposed.
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
