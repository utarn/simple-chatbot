# ReceiptProcessor

## วัตถุประสงค์ (Purpose)
`ReceiptProcessor` มีหน้าที่ในการประมวลผลรูปภาพใบเสร็จที่ผู้ใช้ส่งมา โดยใช้บริการ AI ภายนอก (OpenRouter) ในการทำ OCR เพื่อสกัดข้อมูลสำคัญ เช่น เลขที่ใบเสร็จ และยอดเงิน จากนั้นจะบันทึกข้อมูลที่สกัดได้ลงใน Google Sheet และตอบกลับผู้ใช้ด้วยข้อมูลสรุปของใบเสร็จ

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant ReceiptProcessor
    participant ApplicationDbContext
    participant LineAPI
    participant OpenRouterAPI
    participant ReceiptGoogleSheetHelper
    participant GoogleSheet

    User->>LineWebhookCommand: ส่งรูปภาพใบเสร็จ (Line Image Message)
    LineWebhookCommand->>ReceiptProcessor: เรียกใช้ ProcessLineAsync / ProcessLineImageAsync
    ReceiptProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LineChannelAccessToken)
    ApplicationDbContext-->>ReceiptProcessor: ส่งคืน Chatbot
    ReceiptProcessor->>LineAPI: ดึงเนื้อหารูปภาพ (GetContentAsync)
    LineAPI-->>ReceiptProcessor: ส่งคืน ContentResult (base64 image data)
    ReceiptProcessor->>LineAPI: ดึงชื่อโปรไฟล์ LINE (GetLineProfileName)
    LineAPI-->>ReceiptProcessor: ส่งคืน displayName
    ReceiptProcessor->>ReceiptProcessor: สร้าง Request Body สำหรับ OpenRouter (ReceiptPrompt)
    ReceiptProcessor->>OpenRouterAPI: เรียกใช้ CallOpenRouterApiAsync (เพื่อ OCR ใบเสร็จ)
    OpenRouterAPI-->>ReceiptProcessor: ส่งคืน ReceiptResult (ข้อมูลใบเสร็จ)
    ReceiptProcessor->>ReceiptGoogleSheetHelper: บันทึกข้อมูลลง Google Sheet (AppendRowAsync)
    ReceiptGoogleSheetHelper->>GoogleSheet: บันทึกข้อมูล
    GoogleSheet-->>ReceiptGoogleSheetHelper: ยืนยันการบันทึก
    ReceiptProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมข้อมูลสรุปใบเสร็จ)
    LineWebhookCommand-->>User: แสดงข้อมูลสรุปใบเสร็จและยืนยันการบันทึก
```

## แผนภาพเอนทิตี (Entity Diagram)
(Processor นี้ใช้เอนทิตี `Chatbot` และโมเดลข้อมูลภายในเพื่อจัดเก็บข้อมูลที่สกัดได้ชั่วคราว)

```mermaid
classDiagram
    class Chatbot {
        +int Id
        +string LineChannelAccessToken
    }
    class ReceiptResult {
        +string ReceiptNumber
        +double Amount
        +string LineDisplayName
    }

    ReceiptProcessor ..> Chatbot : ใช้ข้อมูล
    ReceiptProcessor ..> ReceiptResult : สร้างและใช้
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IApplicationDbContext`: ใช้สำหรับเข้าถึงข้อมูลในฐานข้อมูล เช่น `Chatbot` เพื่อดึง `LineChannelAccessToken`
- `IHttpClientFactory`: ใช้สำหรับสร้าง `HttpClient` เพื่อเรียกใช้ LINE API และ OpenRouter API
- `ILogger<ReceiptProcessor>`: ใช้สำหรับบันทึกข้อมูล Log
- `ReceiptGoogleSheetHelper`: คลาสช่วยในการจัดการการบันทึกข้อมูลลง Google Sheet
- `IDistributedCache`: (ปัจจุบันไม่ได้ใช้โดยตรงใน ProcessLineAsync แต่มีอยู่ใน Constructor)
