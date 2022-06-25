using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Threading.Tasks;
using API.Infrastructure.BackgroundTask.BackgroundTaskQueue.Abstract;
using API.Services.Notifications.Email.Abstraction;
using Microsoft.Extensions.Options;
using API.ConfigurationOptions;

namespace API.Services.Notifications.Email.Implementation
{
    public class SendGrid : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderName;
        private readonly string _senderEmail;
        private readonly ILogger<SendGrid> _logger;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public SendGrid(ILogger<SendGrid> logger, IBackgroundTaskQueue backgroundTaskQueue, IOptions<SendGridOptions> options)
        {
            _apiKey = options.Value.ApiKey;
            _senderName = options.Value.SenderName;
            _senderEmail = options.Value.SenderEmail;
            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async Task<bool> SendAsync(IEmailService.Message message)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var subject = message.Subject;
            var receivers = message.Receivers.Select(receiver => new EmailAddress(receiver.Address)).ToList();
            var body = message.Body;
            var emailMessage = MailHelper.CreateSingleEmailToMultipleRecipients(from, receivers, subject, "", body);

            if (message.Attachment != null && message.AttachmentName != null)
            {
                var fileBase64 = Convert.ToBase64String(message.Attachment);
                emailMessage.AddAttachment(message.AttachmentName, fileBase64);
            }

            var response = await client.SendEmailAsync(emailMessage);

            if (!response.IsSuccessStatusCode)
            {
                LogErrorResult(await response.Body.ReadAsStringAsync());
            }

            return response.IsSuccessStatusCode;
        }

        private void LogErrorResult(string text)
        {
            var sendGridException = new Exception(text);

            _logger.LogError(sendGridException, $"[{nameof(SendGrid)}] [UTC] [{DateTime.UtcNow.ToString("dd/MM/yyy HH:mm:ss")}] Send email unsuccessfully completed, problem occurred:");
        }

        public void SendInBackground(IEmailService.Message message)
        {
            _backgroundTaskQueue.EnqueueTask(async (serviceScopeFactory, cancellationToken) =>
            {
                // Get services
                using var scope = serviceScopeFactory.CreateScope();
                var sendGridEmailService = scope.ServiceProvider.GetRequiredService<SendGrid>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SendGrid>>();

                try
                {
                    var isEmailSendSuccess = await sendGridEmailService.SendAsync(message);

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
