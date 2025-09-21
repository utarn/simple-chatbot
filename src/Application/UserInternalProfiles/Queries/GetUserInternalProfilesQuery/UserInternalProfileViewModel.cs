using Utharn.Library.Localizer;

namespace ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfilesQuery;

public class UserInternalProfileViewModel
{
    public int Id { get; set; }
    
    [Localize(Value = "LINE User ID")]
    public string? LineUserId { get; set; }
    
    [Localize(Value = "คำนำหน้า")]
    public string? Initial { get; set; }
    
    [Localize(Value = "ชื่อ")]
    public string? FirstName { get; set; }
    
    [Localize(Value = "นามสกุล")]
    public string? LastName { get; set; }
    
    [Localize(Value = "กลุ่ม")]
    public string? Group { get; set; }
    
    [Localize(Value = "คณะ")]
    public string? Faculty { get; set; }
    
    [Localize(Value = "วิทยาเขต")]
    public string? Campus { get; set; }

    [Localize(Value = "ชื่อเต็ม")]
    public string FullName => $"{Initial ?? ""} {FirstName ?? ""} {LastName ?? ""}".Trim();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Domain.Entities.UserInternalProfile, UserInternalProfileViewModel>();
        }
    }
}