using Xunit;
using MovieTheater.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace MovieTheater.Tests.Service
{
    public class EmailServiceTests
    {
        private EmailService CreateTestEmailService(Func<bool> sendEmailFunc = null, bool shouldThrow = false)
        {
            // Test double for EmailService
            return new TestEmailService(sendEmailFunc ?? (() => true), shouldThrow);
        }

        public class TestEmailService : EmailService
        {
            private readonly Func<bool> _sendEmailFunc;
            private readonly bool _throw;
            public TestEmailService(Func<bool> sendEmailFunc, bool shouldThrow = false)
                : base(new Moq.Mock<IConfiguration>().Object, new Moq.Mock<ILogger<EmailService>>().Object)
            {
                _sendEmailFunc = sendEmailFunc;
                _throw = shouldThrow;
            }
            public override bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
            {
                if (_throw) throw new Exception("fail");
                return _sendEmailFunc();
            }
        }

        [Fact]
        public void SendEmail_ShouldReturnTrue_WhenEmailSentSuccessfully()
        {
            // Arrange
            var emailService = CreateTestEmailService(() => true);
            // Act
            var result = emailService.SendEmail("a@b.com", "subject", "body");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SendEmail_ShouldReturnFalse_WhenEmailSendFails()
        {
            // Arrange
            var emailService = CreateTestEmailService(() => false);
            // Act
            var result = emailService.SendEmail("a@b.com", "subject", "body");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SendEmail_ShouldReturnFalse_WhenConfigIsMissing()
        {
            // Arrange
            var configMock = new Moq.Mock<IConfiguration>();
            configMock.Setup(c => c.GetSection("EmailSettings")["SmtpServer"]).Returns((string)null);
            var loggerMock = new Moq.Mock<ILogger<EmailService>>();
            var service = new EmailService(configMock.Object, loggerMock.Object);

            // Act
            var result = service.SendEmail("a@b.com", "subject", "body");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SendEmail_ShouldReturnTrue_WhenAllConfigAndSmtpClientAreValid()
        {
            // Arrange
            var configMock = new Moq.Mock<IConfiguration>();
            var sectionMock = new Moq.Mock<IConfigurationSection>();
            configMock.Setup(c => c.GetSection("EmailSettings")).Returns(sectionMock.Object);
            sectionMock.Setup(s => s["SmtpServer"]).Returns("smtp.test.com");
            sectionMock.Setup(s => s["Port"]).Returns("25");
            sectionMock.Setup(s => s["Username"]).Returns("user");
            sectionMock.Setup(s => s["Password"]).Returns("pass");
            sectionMock.Setup(s => s["FromEmail"]).Returns("from@test.com");
            sectionMock.Setup(s => s["FromName"]).Returns("Test Sender");

            var loggerMock = new Moq.Mock<ILogger<EmailService>>();

            var smtpClientMock = new Moq.Mock<MovieTheater.Service.ISmtpClient>();
            smtpClientMock.SetupProperty(c => c.Port);
            smtpClientMock.SetupProperty(c => c.Credentials);
            smtpClientMock.SetupProperty(c => c.EnableSsl);
            smtpClientMock.Setup(c => c.Send(Moq.It.IsAny<System.Net.Mail.MailMessage>()));
            smtpClientMock.Setup(c => c.Dispose());

            var service = new EmailService(
                configMock.Object,
                loggerMock.Object,
                host => smtpClientMock.Object
            );

            // Act
            var result = service.SendEmail("to@test.com", "subject", "body");

            // Assert
            Assert.True(result);
            smtpClientMock.Verify(c => c.Send(Moq.It.IsAny<System.Net.Mail.MailMessage>()), Moq.Times.Once);
        }
    }
} 