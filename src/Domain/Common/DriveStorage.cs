namespace ChatbotApi.Domain.Common
{
    public class DriveStorage
    {
        public DateTime UploadDate { get; set; }
        public string CloudFileId { get; set; } = default!;
        public string CloudFileName { get; set; } = default!;
        public string FileType { get; set; } = default!;
        public long FileSize { get; set; }
    }
}
