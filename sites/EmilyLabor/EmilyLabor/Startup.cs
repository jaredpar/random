using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(EmilyLabor.Startup))]
namespace EmilyLabor
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
