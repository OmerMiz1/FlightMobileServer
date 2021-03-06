using FlightMobileWeb.ClientModels;
using FlightMobileWeb.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlightMobileWeb
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var simulatorConfigSection = Configuration.GetSection("SimulatorConfig");
            var simulatorConfig = new SimulatorConfig(simulatorConfigSection);

            services.AddControllers().AddNewtonsoftJson();
            services.AddScoped(typeof(FlightGearController));
            services.AddSingleton(simulatorConfig);
            services.AddSingleton(typeof(IAsyncTcpClient), typeof(FlightGearAsyncClient));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
