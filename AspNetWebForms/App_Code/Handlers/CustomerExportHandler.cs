using System;
using System.Web;
using AspNetWebForms.Data;

namespace AspNetWebForms.Handlers
{
    // Backs Handlers/CustomerExport.ashx. A generic handler writes directly to the response
    // stream with no page lifecycle, master page, or ViewState - the go-to mechanism for
    // file downloads and lightweight endpoints that don't need markup.
    public class CustomerExportHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            var repository = new CustomerRepository();
            var customers = repository.GetCustomers("CustomerId");

            context.Response.ContentType = "text/csv";
            context.Response.AddHeader("Content-Disposition", "attachment; filename=customers.csv");

            context.Response.Write("CustomerId,Name,Email,CreatedDate" + Environment.NewLine);
            foreach (var customer in customers)
            {
                context.Response.Write(string.Format("{0},{1},{2},{3}{4}",
                    customer.CustomerId,
                    EscapeCsv(customer.Name),
                    EscapeCsv(customer.Email),
                    customer.CreatedDate.ToString("yyyy-MM-dd"),
                    Environment.NewLine));
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
