namespace ChatbotApi.Domain.Entities;

public class PreMessageContent
{
    public int Id { get; set; }
    public byte[] Content { get; set; }
    public virtual ICollection<PreMessage> PreMessages { get; }

    public PreMessageContent()
    {
        PreMessages = new List<PreMessage>();
    }
}
