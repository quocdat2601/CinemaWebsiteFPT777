using System.Net.Mail;

namespace MovieTheater.Service
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpClient = new SmtpClient(smtpSettings["SmtpServer"])
                {
                    Port = int.Parse(smtpSettings["Port"]),
                    Credentials = new System.Net.NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = true,
                };

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
                _logger.LogInformation($"Email successfully sent to {toEmail}. Please check your inbox, including the Spam or Junk folder.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}. Error: {ex.Message}");
                return false;
            }
        }
    }
}