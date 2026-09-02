using System.Collections.Generic;
using System.Linq;
using AspNetWebForms.Models;

namespace AspNetWebForms.Data
{
    // Plain "business object" used as the TypeName target for an ObjectDataSource.
    // Each method opens and disposes its own context - there is no shared unit-of-work
    // across a request, which is typical of this era of Web Forms data access code.
    public class CustomerRepository
    {
        public IEnumerable<Customer> GetCustomers(string sortExpression)
        {
            using (var db = new ApplicationDbContext())
            {
                IQueryable<Customer> query = db.Customers;
                switch (sortExpression)
                {
                    case "Name DESC": query = query.OrderByDescending(c => c.Name); break;
                    case "Name": query = query.OrderBy(c => c.Name); break;
                    case "Email DESC": query = query.OrderByDescending(c => c.Email); break;
                    case "Email": query = query.OrderBy(c => c.Email); break;
                    case "CreatedDate DESC": query = query.OrderByDescending(c => c.CreatedDate); break;
                    case "CreatedDate": query = query.OrderBy(c => c.CreatedDate); break;
                    default: query = query.OrderBy(c => c.CustomerId); break;
                }
                return query.ToList();
            }
        }

        public Customer GetCustomerById(int customerId)
        {
            using (var db = new ApplicationDbContext())
            {
                return db.Customers.Include("Orders").FirstOrDefault(c => c.CustomerId == customerId);
            }
        }

        public void UpdateCustomer(int customerId, string name, string email)
        {
            using (var db = new ApplicationDbContext())
            {
                var customer = db.Customers.Find(customerId);
                if (customer == null) return;
                customer.Name = name;
                customer.Email = email;
                db.SaveChanges();
            }
        }

        public void DeleteCustomer(int customerId)
        {
            using (var db = new ApplicationDbContext())
            {
                var customer = db.Customers.Find(customerId);
                if (customer == null) return;
                db.Customers.Remove(customer);
                db.SaveChanges();
            }
        }
    }
}
