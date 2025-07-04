using Utharn.Library.Localizer;

namespace ChatbotApi.Application.PlayLists.Queries.GetPlayListQuery;

public class PlayListMetadata
{
    public int? Id { get; set; }

    [Localize(Value = "วันที่เริ่มต้น")]
    public DateTime? StartDate { get; set; }

    [Localize(Value = "วันที่สิ้นสุด")]
    public DateTime? EndDate { get; set; }

    [Localize(Value = "ค้นหาชื่อเพลงหรืออัลบั้ม")]
    public string? SearchText { get; set; }
}