using EmailWorkerService;
using Microsoft.Extensions.Options;
using Routeco.Data.EmailRepository;
using Routeco.EmailWorkService.Domain;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Routeco.EmailWorkerService
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailRepository emailrepository;
        private readonly IOptions<EmailDetailsConfiguration> emailDetailsConfiguration;

        public EmailSender(IEmailRepository emailrepository, IOptions<EmailDetailsConfiguration> emailDetailsConfiguration)
        {
            this.emailrepository = emailrepository;
            this.emailDetailsConfiguration = emailDetailsConfiguration;
        }
        public async Task SendEmailAsync(EmailDetails details, CancellationToken stoppingToken)
        {

            var mail = new MailMessage();
            try
            {
                mail.From = new MailAddress(details.From, details.FromDisplayName);
                mail.IsBodyHtml = details.IsBodyHtml;
                mail.Body = details.Body;
                mail.Subject = details.Subject;
                AddRecipients(details.Recipients, details.RecipientsDisplayName).ForEach(x => mail.To.Add(x));
                AddRecipients(details.CcRecipients).ForEach(x => mail.CC.Add(x));
                AddRecipients(details.CcRecipients).ForEach(x => mail.Bcc.Add(x));
                await SendMailAsync(mail, details.Id, stoppingToken);
            }
            finally
            {
                if (mail != null)
                {
                    mail.Dispose();
                }
            }
        }

        private async Task SendMailAsync(MailMessage mail, int messageId, CancellationToken stoppingToken)
        {
            var emailSent = false;
            var attempts = 0;
            while (!emailSent && attempts < emailDetailsConfiguration.Value.MaxTries)
            {
                using (var client = new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.Network, EnableSsl = true, Port = emailDetailsConfiguration.Value.Port, Host = emailDetailsConfiguration.Value.Server })
                {
                    if (!string.IsNullOrEmpty(emailDetailsConfiguration.Value.CredentialEmail) && !string.IsNullOrEmpty(emailDetailsConfiguration.Value.Password))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(emailDetailsConfiguration.Value.CredentialEmail, emailDetailsConfiguration.Value.Password);
                    }

                    try
                    {
                        if (emailDetailsConfiguration.Value.Send)
                        {
                            client.Send(mail);
                        }
                        emailSent = true;
                        //Todo: separate try catch email sent delete failed what do we do
                        emailrepository.Delete(messageId);
                    }
                    catch (Exception ex)
                    {
                        attempts++;
                        if (attempts == emailDetailsConfiguration.Value.MaxTries)
                        {
                            emailrepository.MoveToError(messageId, ex.Message);
                        }
                        await Delay(attempts, stoppingToken);
                    }
                }

            }
        }

        private async Task Delay(int attempts, CancellationToken stoppingToken)
        {
            await Task.Delay(500*Convert.ToInt32(Math.Pow(attempts,2)), stoppingToken);
        }

        private List<MailAddress> AddRecipients(List<string> recipients, List<string> DisplayNames)
        {
            var mailRecipients = new List<MailAddress>();
            for (int i = 0; i < recipients.Count; i++)
            {
                mailRecipients.Add(new MailAddress(recipients[i], GetDisplayName(recipients, DisplayNames, i)));
            }
            return mailRecipients;
        }

        private List<MailAddress> AddRecipients(List<string> recipients)
        {
            return AddRecipients(recipients, new List<string>());
        }

        public string GetDisplayName(List<string> recipients, List<string> recipientsDisplayName, int element)
        {
            if ((recipientsDisplayName.Count - 1) >= element)
            {
                return recipientsDisplayName[element];
            }
            return recipients[element];
        }

    }
}
