using System.Net.Mail;

namespace MovieTheater.Service
{
    public interface ISmtpClient : IDisposable
    {
        int Port { get; set; }
        System.Net.ICredentialsByHost Credentials { get; set; }
        bool EnableSsl { get; set; }
        void Send(MailMessage message);
    }

    public class SmtpClientWrapper : ISmtpClient
    {
        private readonly SmtpClient _client;
        public SmtpClientWrapper(string host)
        {
            _client = new SmtpClient(host);
        }
        public int Port { get => _client.Port; set => _client.Port = value; }
        public System.Net.ICredentialsByHost Credentials { get => _client.Credentials; set => _client.Credentials = value; }
        public bool EnableSsl { get => _client.EnableSsl; set => _client.EnableSsl = value; }
        public void Send(MailMessage message) => _client.Send(message);
        public void Dispose() => _client.Dispose();
    }

    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly Func<string, ISmtpClient> _smtpClientFactory;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, Func<string, ISmtpClient> smtpClientFactory = null)
        {
            _configuration = configuration;
            _logger = logger;
            _smtpClientFactory = smtpClientFactory ?? (host => new SmtpClientWrapper(host));
        }

        public virtual bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                using var smtpClient = _smtpClientFactory(smtpSettings["SmtpServer"]);
                smtpClient.Port = int.Parse(smtpSettings["Port"]);
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                smtpClient.EnableSsl = true;

                var fromEmail = smtpSettings["FromEmail"].Trim();
                var fromName = smtpSettings["FromName"].Trim();

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                };
                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
                _logger.LogInformation("Email successfully sent. Please check your inbox, including the Spam or Junk folder.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email. Error: {ex.Message}");
                return false;
            }
        }
    }
}