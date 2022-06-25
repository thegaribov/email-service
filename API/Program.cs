using API.Infrastructure.BackgroundTask.BackgroundTaskQueue.Abstract;
using API.Infrastructure.BackgroundTask.BackgroundTaskQueue.Implementation;
using API.Services.Notifications.Email.Abstraction;
using SendGridEmailService = API.Services.Notifications.Email.Implementation.SendGrid;
using SmtpEmailService = API.Services.Notifications.Email.Implementation.Smtp;
using Microsoft.AspNetCore.Mvc;
using API.DTOs.Email;
using API.ConfigurationOptions;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<SendGridOptions>(builder.Configuration.GetRequiredSection(nameof(SendGridOptions)));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            builder.Services.AddHostedService<BackgroundQueueHostedService>();
            builder.Services.AddTransient<IEmailService, SendGridEmailService>();
            //builder.Services.AddTransient<IEmailService, SmtpEmailService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            MapControllers(app);

            app.Run();
        }

        public static void MapControllers(WebApplication app)
        {
            app.MapPost("/send-individual-email", async (IEmailService emailService, [FromBody] IndividualEmailMessageDTO dto) =>
            {
                await emailService.SendAsync(dto.Subject, dto.Body, dto.Receiver);
            });

            app.MapPost("/send-individual-email-attachment", async (IEmailService emailService, [FromBody] IndividualEmailAttachmentMessageDTO dto) =>
            {
                await emailService.SendAsync(dto.Subject, dto.Body, Convert.FromBase64String(dto.Attachment), dto.AttachmentName, dto.Receiver);
            });

            app.MapPost("/send-bulk-email", async (IEmailService emailService, [FromBody] BulkEmailMessageDTO dto) =>
            {
                await emailService.SendAsync(dto.Subject, dto.Body, dto.Receivers);
            });
        }
    }
}