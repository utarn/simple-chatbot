# FormT1Processor

## วัตถุประสงค์ (Purpose)
`FormT1Processor` มีหน้าที่ประมวลผลข้อความจากผู้ใช้เพื่อสร้างเอกสาร PDF ใบลา (Form T1) โดยใช้ข้อมูลที่สกัดได้จากข้อความของผู้ใช้ผ่านบริการ OpenAI Processor นี้จะรับผิดชอบในการเรียกใช้ OpenAI เพื่อวิเคราะห์ข้อความ, สกัดข้อมูลที่จำเป็นสำหรับการกรอกแบบฟอร์ม, สร้างไฟล์ PDF โดยใช้เทมเพลตที่กำหนดไว้, และส่งคืนลิงก์สำหรับดาวน์โหลดไฟล์ PDF ให้กับผู้ใช้

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant FormT1Processor
    participant OpenAIService
    participant ApplicationDbContext
    participant FileSystem
    participant SystemService

    User->>LineWebhookCommand: ส่งข้อความ (Line Message) เช่น "สร้างใบลา..."
    LineWebhookCommand->>FormT1Processor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    FormT1Processor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LlmKey, ModelName)
    ApplicationDbContext-->>FormT1Processor: ส่งคืน Chatbot
    FormT1Processor->>OpenAIService: เรียกใช้ GetOpenAiResponseAsync(OpenAiRequest) เพื่อสกัดข้อมูล Form T1
    OpenAIService-->>FormT1Processor: ส่งคืน FormT1Response (ข้อมูลที่สกัดได้)
    alt ข้อมูลไม่ครบถ้วนหรือไม่ต้องการสร้างใบลา
        FormT1Processor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมข้อความแจ้งเตือน)
        LineWebhookCommand-->>User: ส่งข้อความแจ้งเตือน
    else ข้อมูลครบถ้วน
        FormT1Processor->>FormT1Processor: เรียกใช้ GeneratePdfForm(templatePath, extractedData)
        FormT1Processor->>FileSystem: อ่านเทมเพลต PDF (formT1.pdf)
        FileSystem-->>FormT1Processor: ส่งคืนเทมเพลต PDF
        FormT1Processor->>FileSystem: เขียนไฟล์ PDF ที่สร้างขึ้นไปยัง /wwwroot/uploads
        FileSystem-->>FormT1Processor: ยืนยันการเขียนไฟล์
        FormT1Processor->>SystemService: ขอ FullHostName
        SystemService-->>FormT1Processor: ส่งคืน FullHostName
        FormT1Processor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมลิงก์ PDF และข้อมูลสรุป)
        LineWebhookCommand-->>User: ส่งข้อความตอบกลับ (ลิงก์ PDF และข้อมูลสรุป)
    end
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
    class FormT1Data {
        +string FullName
        +string FromDate
        +string ToDate
        +string Telephone
        +string Reason
        +string Story
        +string Position
        +string Faculty
        +string WriteAt
        +bool WriteCurrentDate
        +bool NeedToCreate
    }

    FormT1Processor ..> Chatbot : ใช้ข้อมูล
    FormT1Processor ..> FormT1Data : สร้างและใช้
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IApplicationDbContext`: ใช้สำหรับเข้าถึงข้อมูลในฐานข้อมูล เช่น ข้อมูล `Chatbot`
- `IHttpClientFactory`: ใช้สำหรับสร้าง `HttpClient` (ปัจจุบันไม่ได้ใช้โดยตรงใน ProcessLineAsync แต่มีอยู่ใน Constructor)
- `ILogger<FormT1Processor>`: ใช้สำหรับบันทึกข้อมูล Log
- `IWebHostEnvironment`: ใช้สำหรับเข้าถึงพาธของไฟล์เทมเพลต PDF และพาธสำหรับบันทึกไฟล์ที่สร้างขึ้น
- `ISystemService`: ใช้สำหรับดึง `FullHostName` เพื่อสร้าง URL สำหรับไฟล์ PDF ที่สร้างขึ้น
- `IOpenAiService`: ใช้สำหรับสื่อสารกับบริการ OpenAI เพื่อสกัดข้อมูลจากข้อความของผู้ใช้
