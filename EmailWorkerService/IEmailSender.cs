using EmailWorkerService;
using Routeco.EmailWorkService.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Routeco.EmailWorkerService
{
    public interface IEmailSender
    {
        string GetDisplayName(List<string> recipients, List<string> recipientsDisplayName, int element);
        Task SendEmailAsync(EmailDetails details, CancellationToken stoppingToken);
    }
}