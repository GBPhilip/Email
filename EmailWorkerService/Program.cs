using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Routeco.Data.EmailRepository;
using Routeco.EmailWorkerService;
using Serilog;
using Serilog.Formatting.Compact;
using System;

namespace EmailWorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.File(new RenderedCompactJsonFormatter(), @"e:\temp\EmailService.log")
                .CreateLogger();

            try
            {
                Log.Information("Starting up at {time}", DateTimeOffset.Now);
                CreateHostBuilder(args).Build().Run();

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<Program>();
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    services.Configure<EmailDetailsConfiguration>(configuration.GetSection("Email"));
                    services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
                    services.AddTransient<IEmailRepository, EmailRepository>();
                    services.AddTransient<IEmailSender, EmailSender>();
                    services.AddHostedService<Worker>();
                })
            .UseSerilog();
    }
}
