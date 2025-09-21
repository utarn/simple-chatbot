using Utharn.Library.Localizer;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.UserInternalProfiles.Queries.GetUserInternalProfileByIdQuery;

public class UserInternalProfileSingleViewModel
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

    public static UserInternalProfileSingleViewModel MappingFunction(UserInternalProfile profile)
    {
        return new UserInternalProfileSingleViewModel
        {
            Id = profile.Id,
            LineUserId = profile.LineUserId,
            Initial = profile.Initial,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Group = profile.Group,
            Faculty = profile.Faculty,
            Campus = profile.Campus
        };
    }
}