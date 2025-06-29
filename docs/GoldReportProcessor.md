# GoldReportProcessor

## วัตถุประสงค์ (Purpose)
`GoldReportProcessor` มีหน้าที่ในการดึงข้อมูลราคาทองคำจากเว็บไซต์ภายนอก (`https://ทองคําราคา.com/`) จากนั้นใช้บริการ OpenAI เพื่อสกัดและจัดรูปแบบข้อมูลราคาทองคำที่ได้มาให้อยู่ในรูปแบบที่เข้าใจง่าย และส่งกลับเป็นข้อความตอบกลับไปยังผู้ใช้ผ่าน LINE หรือช่องทางอื่น ๆ

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant GoldReportProcessor
    participant ExternalWebsite
    participant OpenAIService
    participant ApplicationDbContext

    User->>LineWebhookCommand: ส่งข้อความ (Line Message) เช่น "ราคาทองวันนี้"
    LineWebhookCommand->>GoldReportProcessor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    GoldReportProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LlmKey, ModelName)
    ApplicationDbContext-->>GoldReportProcessor: ส่งคืน Chatbot
    GoldReportProcessor->>ExternalWebsite: ดึงข้อมูล HTML จาก https://ทองคําราคา.com/
    ExternalWebsite-->>GoldReportProcessor: ส่งคืน HTML Content
    GoldReportProcessor->>OpenAIService: ส่ง HTML Content และ Prompt เพื่อสกัดข้อมูลราคาทองคำ
    OpenAIService-->>GoldReportProcessor: ส่งคืน JSON ข้อมูลราคาทองคำ (GoldReport)
    GoldReportProcessor->>GoldReportProcessor: จัดรูปแบบข้อมูลราคาทองคำ (FormatGoldReport)
    GoldReportProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมข้อความราคาทองคำ)
    LineWebhookCommand-->>User: ส่งข้อความตอบกลับ (ราคาทองคำ)
```

## แผนภาพเอนทิตี (Entity Diagram)
(Processor นี้ไม่ได้จัดการเอนทิตีโดยตรง แต่ใช้เอนทิตี `Chatbot` และโมเดลข้อมูลชั่วคราวสำหรับการสกัดข้อมูล)

```mermaid
classDiagram
    class Chatbot {
        +int Id
        +string LlmKey
        +string ModelName
    }
    class GoldReport {
        +string Title
        +string GoldPurity
        +Prices Prices
        +DailyChange DailyChange
        +UpdateDetails UpdateDetails
    }
    class Prices {
        +GoldBar GoldBar
        +OrnamentalGold OrnamentalGold
    }
    class GoldBar {
        +string Type
        +float BidPrice
        +float AskPrice
    }
    class OrnamentalGold {
        +string Type
        +float BidPrice
        +float AskPrice
    }
    class DailyChange {
        +int Amount
        +string Direction
    }
    class UpdateDetails {
        +string Date
        +string Time
        +int Revision
    }

    GoldReportProcessor ..> Chatbot : ใช้ข้อมูล
    GoldReportProcessor ..> GoldReport : สร้างและใช้
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IHttpClientFactory`: ใช้สำหรับสร้าง `HttpClient` เพื่อดึงข้อมูลจากเว็บไซต์ราคาทองคำ
- `IOpenAiService`: ใช้สำหรับสื่อสารกับบริการ OpenAI เพื่อสกัดข้อมูลจาก HTML
- `IApplicationDbContext`: ใช้สำหรับเข้าถึงข้อมูลในฐานข้อมูล เช่น ข้อมูล `Chatbot`
