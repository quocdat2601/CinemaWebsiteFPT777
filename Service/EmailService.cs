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

                // Early validation to avoid creating SmtpClient if configuration is invalid
                if (smtpSettings == null)
                {
                    _logger.LogError("EmailSettings configuration section is missing");
                    return false;
                }

                var smtpServer = smtpSettings["SmtpServer"];
                if (string.IsNullOrEmpty(smtpServer))
                {
                    _logger.LogError("SmtpServer configuration is missing or empty");
                    return false;
                }

                var portStr = smtpSettings["Port"];
                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out var port))
                {
                    _logger.LogError("Port configuration is missing or invalid");
                    return false;
                }

                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"]?.Trim();
                var fromName = smtpSettings["FromName"]?.Trim();

                if (string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("FromEmail configuration is missing or empty");
                    return false;
                }

                using var smtpClient = _smtpClientFactory(smtpServer);
                smtpClient.Port = port;
                smtpClient.Credentials = new System.Net.NetworkCredential(username, password);
                smtpClient.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName ?? fromEmail),
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
                _logger.LogError(ex, "Failed to send email");
                return false;
            }
        }
    }
}