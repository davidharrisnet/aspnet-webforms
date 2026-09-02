using System.Data.Entity.Migrations;
using AspNetWebForms.Models;

namespace AspNetWebForms.Migrations
{
    // This is the configuration half of EF6 Code First Migrations - it compiles and
    // documents the pattern, but the actual migration snapshot files
    // (<timestamp>_InitialCreate.cs/.Designer.cs/.resx) are not included here. Those are
    // scaffolded by EF6's tooling running inside Visual Studio's Package Manager Console
    // (Enable-Migrations, then Add-Migration InitialCreate), which isn't available
    // headlessly in this environment - the .resx in particular embeds a compressed
    // snapshot of the compiled model that can't be hand-authored correctly.
    //
    // Until that's run, this app keeps using EF6's default CreateDatabaseIfNotExists
    // initializer (LocalDB creates the schema straight from the current model on first
    // access - see ApplicationDbContext). Seed() below shows where enterprise apps
    // typically load reference/lookup data once migrations are wired up.
    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            context.Customers.AddOrUpdate(
                c => c.Email,
                new Customer { Name = "Contoso Ltd", Email = "ap@contoso.example" },
                new Customer { Name = "Fabrikam Inc", Email = "billing@fabrikam.example" });
        }
    }
}
