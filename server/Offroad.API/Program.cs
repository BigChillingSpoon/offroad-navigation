using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Offroad.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =========================
            // Services
            // =========================

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // =========================
            // Build app
            // =========================

            var app = builder.Build();

            // =========================
            // HTTP pipeline
            // =========================

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // =========================
            // Endpoints
            // =========================

            app.MapGet("/health", () => Results.Ok("OK"))
               .WithName("Health");

            app.Run();
        }
    }
}
