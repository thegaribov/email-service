using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services.Notifications.Email.Abstraction
{
    public interface IEmailService
    {
        public class Message
        {
            public List<MailboxAddress> Receivers { get; set; } = new List<MailboxAddress>();
            public string Subject { get; set; }
            public string Body { get; set; }
            public byte[] Attachment { get; set; }
            public string AttachmentName { get; set; }

            public Message(string subject, string body, List<string> receivers)
            {
                Subject = subject;
                Body = body;
                Receivers.AddRange(receivers.Select(recipent => new MailboxAddress(string.Empty, recipent)));
            }

            public Message(string subject, string body, byte[] attachment, string attachmentName, List<string> receivers)
                : this(subject, body, receivers)
            {
                Attachment = attachment;
                AttachmentName = attachmentName;
            }
        }

        Task<bool> SendAsync(Message message);
        Task<bool> SendAsync(string subject, string body, params string[] receivers)
        {
            var message = new Message(subject, body, receivers.ToList());

            return SendAsync(message);
        }
        Task<bool> SendAsync(string subject, string body, byte[] attachment, string attachmentName, params string[] receiver)
        {
            var message = new Message(subject, body, attachment, attachmentName, receiver.ToList());

            return SendAsync(message);
        }

        void SendInBackground(Message message);
        void SendInBackground(string subject, string body, params string[] receivers)
        {
            var message = new Message(subject, body, receivers.ToList());

            SendInBackground(message);
        }
        void SendInBackground(string subject, string body, byte[] attachment, string attachmentName, params string[] receivers)
        {
            var message = new Message(subject, body, attachment, attachmentName, receivers.ToList());

            SendInBackground(message);
        }
    }
}
