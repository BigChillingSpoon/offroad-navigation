using Routing.Infrastructure;
using Routing.Application;
using Routing.Domain;

namespace Offroad.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            RegisterServices(builder);

            var app = builder.Build();

            ConfigurePipeline(app);

            app.Run();
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            // Framework Services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Project Dependencies
            builder.Services.AddRoutingApplication();
            builder.Services.AddRoutingInfrastructure();
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Development tools
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global Middleware
            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Endpoint Mapping
            app.MapControllers();

            app.MapGet("/health", () => Results.Ok("OK"))
               .WithName("Health");
        }
    }
}