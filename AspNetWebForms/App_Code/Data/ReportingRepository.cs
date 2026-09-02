using System.Linq;

namespace AspNetWebForms.Data
{
    public class DashboardSummary
    {
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ReportingRepository
    {
        // Deliberately expensive-looking aggregate query - the kind of thing OutputCache
        // exists to shield the database from on a high-traffic dashboard page.
        public DashboardSummary GetSummary()
        {
            using (var db = new ApplicationDbContext())
            {
                return new DashboardSummary
                {
                    CustomerCount = db.Customers.Count(),
                    OrderCount = db.Orders.Count(),
                    TotalRevenue = db.Orders.Any() ? db.Orders.Sum(o => o.Amount) : 0m
                };
            }
        }
    }
}
