using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using API.Services.Notifications.Email.Abstraction;
using System;
using System.Linq;
using API.Infrastructure.BackgroundTask.BackgroundTaskQueue.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace API.Services.Notifications.Email.Implementation
{
    public class Smtp : IEmailService
    {
        ///If your SMTP host is gmail then don't forget to
        ///enable "Less secure app access" from settings
        private readonly string _senderEmail;
        private readonly string _senderEmailPassword;
        private readonly string _senderHost;
        private readonly int _port;

        private readonly ILogger<Smtp> _logger;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public Smtp(
            ILogger<Smtp> logger,
            IBackgroundTaskQueue backgroundTaskQueue)

        {
            _senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
            _senderEmailPassword = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL_PASSWORD");
            _senderHost = Environment.GetEnvironmentVariable("SMTP_SENDER_HOST");
            _port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));

            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async Task<bool> SendAsync(IEmailService.Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(string.Empty, _senderEmail));
            emailMessage.To.AddRange(message.Receivers);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Body };

            return await SendEmailAsync(emailMessage);
        }
        private async Task<bool> SendEmailAsync(MimeMessage mailMessage)
        {
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_senderHost, _port, true);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    await client.AuthenticateAsync(_senderEmail, _senderEmailPassword);

                    var result = await client.SendAsync(mailMessage);

                    return true;
                }
                catch (Exception smtpException)
                {
                    _logger.LogError(smtpException, $"[{nameof(Smtp)}] [UTC] [{DateTime.UtcNow.ToString("dd/MM/yyy HH:mm:ss")}] Send email unsuccessfully completed, exception occurred.");

                    return false;
                }
                finally
                {
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
        }

        public void SendInBackground(IEmailService.Message message)
        {
            _backgroundTaskQueue.EnqueueTask(async (serviceScopeFactory, cancellationToken) =>
            {
                // Get services
                using var scope = serviceScopeFactory.CreateScope();
                var smtpEmailService = scope.ServiceProvider.GetRequiredService<Smtp>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Smtp>>();

                try
                {
                    var isEmailSendSuccess = await smtpEmailService.SendAsync(message);

                    if (isEmailSendSuccess)
                    {
                        logger.LogInformation($"[BT] [UTC] [{DateTime.UtcNow.ToString("dd/MM/yyy HH:mm:ss")}] Send email completed successfully.");
                    }
                    else
                    {
                        logger.LogWarning($"[BT] [UTC] [{DateTime.UtcNow.ToString("dd/MM/yyy HH:mm:ss")}] Send email completed unsuccessfully, there are some problem, please check it");
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"[BT] [UTC] [{DateTime.UtcNow.ToString("dd/MM/yyy HH:mm:ss")}] Send email completed unsuccessfully, exception occurred.");
                }
            });
        }

    }
}
