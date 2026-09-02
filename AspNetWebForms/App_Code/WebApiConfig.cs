using System.Web.Http;

namespace AspNetWebForms
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Entity navigation properties (Customer.Orders <-> Order.Customer) would
            // otherwise trip JSON.NET's circular-reference detection if ever serialized
            // directly; controllers here return DTOs instead, but this is left in place
            // as the standard defensive default for a Web API layer sitting on top of EF.
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        }
    }
}
