using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AspNetWebForms.Data;
using AspNetWebForms.Models;

namespace AspNetWebForms.Controllers
{
    // Reachable at /api/customers and /api/customers/{id}. Runs entirely alongside the
    // Web Forms pages in the same app - Web API and Web Forms share the same app pool,
    // Web.config, and IIS pipeline, they just have separate routing/dispatch layers.
    public class CustomersApiController : ApiController
    {
        private readonly CustomerRepository _repository = new CustomerRepository();

        // GET api/customers
        public IEnumerable<CustomerDto> Get()
        {
            return _repository.GetCustomers("CustomerId").Select(ToDto);
        }

        // GET api/customers/5
        public IHttpActionResult Get(int id)
        {
            var customer = _repository.GetCustomerById(id);
            if (customer == null)
            {
                return NotFound();
            }
            return Ok(ToDto(customer));
        }

        private static CustomerDto ToDto(Customer customer)
        {
            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                Email = customer.Email,
                CreatedDate = customer.CreatedDate
            };
        }
    }
}
