using Utharn.Library.Localizer;

namespace ChatbotApi.Application.PlayLists.Queries.GetPlayListQuery;

public class PlayListViewModel
{
    public int Id { get; set; }

    [Localize(Value = "ชื่อเพลง")]
    public string MusicName { get; set; } = default!;

    [Localize(Value = "ชื่ออัลบั้ม")]
    public string AlbumName { get; set; } = default!;

    [Localize(Value = "วันที่สร้าง")]
    public DateTime CreatedDate { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.PlayList, PlayListViewModel>();
        }
    }
}