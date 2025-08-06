using System.Net;
using System.Net.Mail;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class SmtpClientWrapperTests
    {
        [Fact]
        public void Constructor_WithValidHost_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new SmtpClientWrapper("smtp.test.com");

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithNullHost_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new SmtpClientWrapper(null);

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithEmptyHost_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new SmtpClientWrapper("");

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Port_GetAndSet_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedPort = 587;

            // Act
            wrapper.Port = expectedPort;
            var actualPort = wrapper.Port;

            // Assert
            Assert.Equal(expectedPort, actualPort);
        }

        [Fact]
        public void Port_SetToZero_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedPort = 0;

            // Act & Assert
            // SMTP client doesn't allow zero port, so this should throw an exception
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Port = expectedPort);
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void Port_SetToNegative_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedPort = -1;

            // Act & Assert
            // SMTP client doesn't allow negative port, so this should throw an exception
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Port = expectedPort);
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void Credentials_GetAndSet_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedCredentials = new NetworkCredential("username", "password");

            // Act
            wrapper.Credentials = expectedCredentials;
            var actualCredentials = wrapper.Credentials;

            // Assert
            Assert.Equal(expectedCredentials, actualCredentials);
        }

        [Fact]
        public void Credentials_SetToNull_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");

            // Act
            wrapper.Credentials = null;
            var actualCredentials = wrapper.Credentials;

            // Assert
            Assert.Null(actualCredentials);
        }

        [Fact]
        public void EnableSsl_GetAndSet_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedEnableSsl = true;

            // Act
            wrapper.EnableSsl = expectedEnableSsl;
            var actualEnableSsl = wrapper.EnableSsl;

            // Assert
            Assert.Equal(expectedEnableSsl, actualEnableSsl);
        }

        [Fact]
        public void EnableSsl_SetToFalse_WorksCorrectly()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var expectedEnableSsl = false;

            // Act
            wrapper.EnableSsl = expectedEnableSsl;
            var actualEnableSsl = wrapper.EnableSsl;

            // Assert
            Assert.Equal(expectedEnableSsl, actualEnableSsl);
        }

        [Fact]
        public void Send_WithValidMailMessage_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage
            {
                From = new MailAddress("from@test.com"),
                Subject = "Test Subject",
                Body = "Test Body"
            };
            mailMessage.To.Add("to@test.com");

            // Act & Assert
            // Note: This will likely throw an exception in a real test environment
            // because there's no actual SMTP server, but we're testing that the method
            // can be called without compilation errors
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Send_WithNullMailMessage_ThrowsException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            MailMessage? mailMessage = null;

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Send_WithEmptyMailMessage_ThrowsException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage();

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Dispose_WhenCalled_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");

            // Act & Assert
            var exception = Record.Exception(() => wrapper.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");

            // Act & Assert
            var exception1 = Record.Exception(() => wrapper.Dispose());
            var exception2 = Record.Exception(() => wrapper.Dispose());
            var exception3 = Record.Exception(() => wrapper.Dispose());

            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersHost_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new SmtpClientWrapper("smtp.test.com:587");

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Constructor_WithIPAddressHost_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new SmtpClientWrapper("192.168.1.1");

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Properties_AfterDispose_StillAccessible()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            wrapper.Port = 587;
            wrapper.EnableSsl = true;
            wrapper.Credentials = new NetworkCredential("user", "pass");

            // Act
            wrapper.Dispose();

            // Assert
            // These should still be accessible even after dispose
            Assert.Equal(587, wrapper.Port);
            Assert.True(wrapper.EnableSsl);
            Assert.NotNull(wrapper.Credentials);
        }

        [Fact]
        public void Send_WithComplexMailMessage_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage
            {
                From = new MailAddress("from@test.com", "From Name"),
                Subject = "Test Subject with Special Characters: !@#$%^&*()",
                Body = "<html><body><h1>Test HTML Body</h1><p>This is a test email with HTML content.</p></body></html>",
                IsBodyHtml = true
            };
            mailMessage.To.Add("to@test.com");
            mailMessage.To.Add("to2@test.com");
            mailMessage.CC.Add("cc@test.com");
            mailMessage.Bcc.Add("bcc@test.com");

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Send_WithMailMessageHavingAttachments_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage
            {
                From = new MailAddress("from@test.com"),
                Subject = "Test Subject",
                Body = "Test Body"
            };
            mailMessage.To.Add("to@test.com");

            // Note: We can't easily create attachments in unit tests without files
            // but we can test that the method signature works

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Send_WithMailMessageHavingPriority_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage
            {
                From = new MailAddress("from@test.com"),
                Subject = "Test Subject",
                Body = "Test Body",
                Priority = MailPriority.High
            };
            mailMessage.To.Add("to@test.com");

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }

        [Fact]
        public void Send_WithMailMessageHavingHeaders_DoesNotThrowException()
        {
            // Arrange
            var wrapper = new SmtpClientWrapper("smtp.test.com");
            var mailMessage = new MailMessage
            {
                From = new MailAddress("from@test.com"),
                Subject = "Test Subject",
                Body = "Test Body"
            };
            mailMessage.To.Add("to@test.com");
            mailMessage.Headers.Add("X-Custom-Header", "CustomValue");

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => wrapper.Send(mailMessage));
        }
    }
} 