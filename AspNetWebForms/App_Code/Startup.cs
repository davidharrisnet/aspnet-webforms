using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AspNetWebForms.Startup))]
namespace AspNetWebForms
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
