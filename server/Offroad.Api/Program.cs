using Offroad.Api.Infrastructure;
using Routing.Infrastructure;
using Routing.Application;
using Routing.Domain;
using Serilog;
using Serilog.Formatting.Compact;

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
            // Logging
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            });

            // Framework Services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Exception Handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // Project Dependencies
            builder.Services.AddRoutingApplication();
            builder.Services.AddRoutingInfrastructure(builder.Configuration);
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
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Endpoint Mapping
            app.MapControllers();

            app.MapGet("/health", () => Results.Ok("OK"))
               .WithName("Health");
        }
    }
}