# CheckCheatOnlineProcessor

## วัตถุประสงค์ (Purpose)
`CheckCheatOnlineProcessor` มีหน้าที่หลักในการประมวลผลข้อความจากผู้ใช้เพื่อตรวจจับข้อมูลที่เกี่ยวข้องกับการฉ้อโกง เช่น หมายเลขโทรศัพท์ บัญชีธนาคาร หรือ URL เว็บไซต์ จากนั้นจะทำการตรวจสอบข้อมูลดังกล่าวกับบริการภายนอก (OpenAI และ CheckGon API) เพื่อยืนยันว่าเป็นข้อมูลที่เกี่ยวข้องกับการฉ้อโกงหรือไม่ และตอบกลับผู้ใช้ด้วยผลลัพธ์ที่เหมาะสม

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant CheckCheatOnlineProcessor
    participant OpenAIService
    participant CheckGonAPI
    participant ApplicationDbContext

    User->>LineWebhookCommand: ส่งข้อความ (Line Message)
    LineWebhookCommand->>CheckCheatOnlineProcessor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ ProcessUserMessage(chatbotId, message)
    CheckCheatOnlineProcessor->>OpenAIService: เรียกใช้ DetectIntentionAsync(chatbotId, message) เพื่อตรวจจับเจตนาและข้อมูล (เบอร์โทร, บัญชี, URL)
    OpenAIService-->>CheckCheatOnlineProcessor: ส่งคืน UserIntention (ข้อมูลที่ตรวจจับได้)
    CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ GetReplyMessage(chatbotId, checkType, rawValue, formattedValue)
    CheckCheatOnlineProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LlmKey, ModelName)
    ApplicationDbContext-->>CheckCheatOnlineProcessor: ส่งคืน Chatbot
    CheckCheatOnlineProcessor->>OpenAIService: เรียกใช้ GetOpenAiResponseAsync(OpenAiRequest) เพื่อตรวจสอบข้อมูลฉ้อโกง
    OpenAIService-->>CheckCheatOnlineProcessor: ส่งคืน ScamCheckResult (ผลการตรวจสอบจาก OpenAI)
    alt พบข้อมูลฉ้อโกง
        CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ BuildFoundReply()
    else ไม่พบข้อมูลฉ้อโกง
        CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ BuildNotFoundReply()
    end
    CheckCheatOnlineProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมข้อความตอบกลับ)
    LineWebhookCommand-->>User: ส่งข้อความตอบกลับ (Line Reply)

    User->>OpenAIService: ส่งข้อความ (OpenAI Message)
    OpenAIService->>CheckCheatOnlineProcessor: เรียกใช้ ProcessOpenAiAsync(chatbotId, messages)
    CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ ProcessUserMessage(chatbotId, userMessageContent)
    CheckCheatOnlineProcessor->>OpenAIService: เรียกใช้ DetectIntentionAsync(chatbotId, userMessageContent)
    OpenAIService-->>CheckCheatOnlineProcessor: ส่งคืน UserIntention
    CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ GetReplyMessage(chatbotId, checkType, rawValue, formattedValue)
    CheckCheatOnlineProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot
    ApplicationDbContext-->>CheckCheatOnlineProcessor: ส่งคืน Chatbot
    CheckCheatOnlineProcessor->>OpenAIService: เรียกใช้ GetOpenAiResponseAsync(OpenAiRequest)
    OpenAIService-->>CheckCheatOnlineProcessor: ส่งคืน ScamCheckResult
    alt พบข้อมูลฉ้อโกง
        CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ BuildFoundReply()
    else ไม่พบข้อมูลฉ้อโกง
        CheckCheatOnlineProcessor->>CheckCheatOnlineProcessor: เรียกใช้ BuildNotFoundReply()
    end
    CheckCheatOnlineProcessor-->>OpenAIService: ส่งคืน OpenAIResponse (พร้อมข้อความตอบกลับ)
```

## แผนภาพเอนทิตี (Entity Diagram)
(เนื่องจาก `CheckCheatOnlineProcessor` ไม่ได้จัดการเอนทิตีโดยตรง แต่ใช้เอนทิตีที่มีอยู่และโมเดลข้อมูลชั่วคราว จึงไม่มีแผนภาพเอนทิตีเฉพาะสำหรับ Processor นี้ อย่างไรก็ตาม มีการใช้งานเอนทิตี `Chatbot` และโมเดลข้อมูลภายในดังนี้)

```mermaid
classDiagram
    class Chatbot {
        +int Id
        +string LlmKey
        +string ModelName
    }
    class UserIntention {
        +string PhoneNumber
        +string BankAccount
        +string WebsiteUrl
        +bool IsGreeting
        +bool IsStoryTelling
        +bool IsQuestion
    }
    class ScamCheckResult {
        +bool IsScam
        +string Description
        +string CaseType
        +string CaseSeverity
        +string InformDate
        +decimal DamagePrice
    }
    class CheckGonCaseData {
        +string Description
        +string CaseType
        +string CaseSeverity
        +DateTime InformDate
        +decimal DamagePrice
    }

    CheckCheatOnlineProcessor ..> Chatbot : ใช้ข้อมูล
    CheckCheatOnlineProcessor ..> UserIntention : สร้างและใช้
    CheckCheatOnlineProcessor ..> ScamCheckResult : สร้างและใช้
    CheckCheatOnlineProcessor ..> CheckGonCaseData : สร้างและใช้
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IApplicationDbContext`: ใช้สำหรับเข้าถึงข้อมูลในฐานข้อมูล เช่น ข้อมูล `Chatbot`
- `IHttpClientFactory`: ใช้สำหรับสร้าง `HttpClient` เพื่อเรียกใช้ CheckGon API
- `ILogger<CheckCheatOnlineProcessor>`: ใช้สำหรับบันทึกข้อมูล Log
- `IOpenAiService`: ใช้สำหรับสื่อสารกับบริการ OpenAI เพื่อตรวจจับเจตนาและตรวจสอบข้อมูลฉ้อโกง
