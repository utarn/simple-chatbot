namespace ChatbotApi.Domain.Constants;

public abstract class Systems
{
    public const int PageSize = 20;
    public const string GoldReport = "GoldReport";
    public const string CustomJSON = "CustomJSON";
    public const string CheckCheatOnline = "CheckCheatOnline";
    public const string LlamaPassport = "LlamaPassport";
    public const string FormT1 = "FormT1";
    public const string Receipt = "Receipt";
    public const string TrackFile = "TrackFileProcessor";
    public const string SummarizeEmail = "SummarizeEmail";
    public static readonly IReadOnlyDictionary<string, string> Plugins = new Dictionary<string, string>
    {
        { CustomJSON, "เปิดการใช้งาน Line Flex หรือ Custom JSON (Line, GoogleChat)" },
        { CheckCheatOnline, "ตรวจสอบการโกงออนไลน์ (Line)" },
        { LlamaPassport, "Llama Passport (Line)" },
        { FormT1, "สร้างใบลาป่วย ลาพักผ่อน ลาคลอดบุตร (Line)" },
        { Receipt, "สร้างใบเสร็จรับเงิน (Line)" },
        { TrackFile, "จัดการไฟล์แนบ (Line)" },
        { GoldReport, "รายงานราคาทองคำ (Line)" },
        { SummarizeEmail, "สรุปเนื้อหาอีเมล (Line)" }
    };
    public const string Echo = "EchoProcessor";
    static Systems()
    {
        Plugins = new Dictionary<string, string>(Plugins)
        {
            { Echo, "Echo Bot (Line): ตอบกลับข้อความพร้อมวันที่และเวลา" }
        };
    }
}
