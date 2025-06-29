using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChatbotApi.Domain.Constants;

public static class SystemList
{
    private static readonly List<SelectListItem> isLineSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "true", Text = "เปิดใช้งาน" },
        new SelectListItem() { Value = "false", Text = "ปิดใช้งาน" }
    };

    public static SelectList IsLineSelectList(this object? value)
    {
        return new SelectList(isLineSelectListItems, "Value", "Text", value);
    }

    private static readonly List<SelectListItem> MethodSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "line", Text = "Line" },
        new SelectListItem() { Value = "google", Text = "Google Chat" }
    };

    public static SelectList MethodSelectList(this object? value)
    {
        return new SelectList(MethodSelectListItems, "Value", "Text", value);
    }

    private static readonly List<SelectListItem> ShowReferenceSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "true", Text = "แสดงไฟล์ที่เกี่ยวข้อง" },
        new SelectListItem() { Value = "false", Text = "ไม่แสดงไฟล์ที่เกี่ยวข้อง" }
    };

    public static SelectList ShowReferenceSelectList(this object? value)
    {
        return new SelectList(ShowReferenceSelectListItems, "Value", "Text", value);
    }

    private static readonly List<SelectListItem> AllowOutsideKnowledgeSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "true", Text = "อนุญาตใช้องค์ความรู้ของโมเดล LLM" },
        new SelectListItem() { Value = "false", Text = "ไม่อนุญาตใช้องค์ความรู้ของโมเดล LLM" },
    };

    public static SelectList AllowOutsideKnowledgeSelectList(this object? value)
    {
        return new SelectList(AllowOutsideKnowledgeSelectListItems, "Value", "Text", value);
    }

    private static readonly List<SelectListItem> ResponsiveAgentSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "true", Text = "ตอบคำถามเมื่อถาม และถามต่อเมื่อตอบ" },
        new SelectListItem() { Value = "false", Text = "ตอบคำถามอย่างเดียว" }
    };

    public static SelectList ResponsiveAgentSelectList(this object? value)
    {
        return new SelectList(ResponsiveAgentSelectListItems, "Value", "Text", value);
    }

    private static readonly List<SelectListItem> EnableWebSearchToolSelectListItems = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "true", Text = "เปิดใช้งาน" },
        new SelectListItem() { Value = "false", Text = "ปิดใช้งาน" }
    };
    
    public static SelectList EnableWebSearchToolSelectList(this object? value)
    {
        return new SelectList(EnableWebSearchToolSelectListItems, "Value", "Text", value);
    }
        
}
