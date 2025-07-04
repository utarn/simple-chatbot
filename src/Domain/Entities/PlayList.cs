namespace ChatbotApi.Domain.Entities;

public class PlayList
{
    public int Id { get; set; }
    public string MusicName { get; set; } = default!;
    public string AlbumName { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
}