using System;
using System.Collections.Generic;

namespace Routeco.EmailWorkService.Domain
{
    public class EmailDetails
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public string From { get; set; }
        public string FromDisplayName { get; set; }
        public List<string> Recipients { get; set; }
        public List<string> RecipientsDisplayName { get; set; }
        public List<string> CcRecipients { get; set; }
        public List<string> BccRecipients { get; set; }

        public EmailDetails()
        {
            Recipients = new List<string>();
            RecipientsDisplayName = new List<string>();
            CcRecipients = new List<string>();
            BccRecipients = new List<string>();
        }
    }
}