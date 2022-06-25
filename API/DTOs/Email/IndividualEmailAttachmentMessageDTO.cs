namespace API.DTOs.Email
{
    public class IndividualEmailAttachmentMessageDTO
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Receiver { get; set; }
        public string Attachment { get; set; }
        public string AttachmentName { get; set; }
    }
}
