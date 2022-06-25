namespace API.DTOs.Email
{
    public class BulkEmailAttachmentMessageDTO
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string[] Receivers { get; set; }
    }
}
