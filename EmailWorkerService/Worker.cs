using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Routeco.Data.EmailRepository;
using Routeco.EmailWorkerService;
using Routeco.EmailWorkService.Domain;

namespace EmailWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEmailRepository emailRepository;
        private readonly IEmailSender emailSender;

        public Worker(ILogger<Worker> logger, IEmailRepository emailRepository, IEmailSender emailSender)
        {
            _logger = logger;
            this.emailRepository = emailRepository;
            this.emailSender = emailSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var emailRequest = emailRepository.Read();
                while (emailRequest != null)
                {
                    _logger.LogInformation("Processing email id {id} at {time}", emailRequest.Id, DateTimeOffset.Now);
                    var emailDetails = JsonConvert.DeserializeObject<EmailDetails>(emailRequest.Message);
                    emailDetails.Id = emailRequest.Id;
                    await emailSender.SendEmailAsync(emailDetails, stoppingToken);
                    emailRequest = emailRepository.Read();
                }
                _logger.LogInformation("No emails queued at {time}", DateTimeOffset.Now);
                await Task.Delay(7000, stoppingToken);
            }
        }
    }
}
