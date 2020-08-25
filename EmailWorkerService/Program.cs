using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Routeco.Data.EmailRepository;
using Routeco.EmailWorkerService;

namespace EmailWorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    services.Configure<EmailDetailsConfiguration>(configuration.GetSection("Email"));
                    services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
                    services.AddTransient<IEmailRepository, EmailRepository>();
                    services.AddTransient<IEmailSender, EmailSender>();
                    services.AddHostedService<Worker>();
                });
    }
}
