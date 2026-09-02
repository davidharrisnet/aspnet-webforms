using System.Collections.Generic;
using System.Linq;
using AspNetWebForms.Models;

namespace AspNetWebForms.Data
{
    public class OrderRepository
    {
        public IEnumerable<Order> GetOrdersByCustomer(int customerId)
        {
            using (var db = new ApplicationDbContext())
            {
                return db.Orders
                    .Where(o => o.CustomerId == customerId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
        }
    }
}
