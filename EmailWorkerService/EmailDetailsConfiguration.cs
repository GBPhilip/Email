namespace Routeco.EmailWorkerService
{
    public class EmailDetailsConfiguration
    {
        public string CredentialEmail { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public bool Send { get; set; }

        public int MaxTries { get; set; }
    }
}