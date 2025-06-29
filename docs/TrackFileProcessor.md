# TrackFileProcessor

## วัตถุประสงค์ (Purpose)
`TrackFileProcessor` มีหน้าที่จัดการการอัปโหลดและค้นหาไฟล์ตามคำอธิบายที่ผู้ใช้ให้มา โดยจะรับไฟล์จาก LINE, ใช้บริการ OpenAI เพื่อสร้าง Embedding จากคำอธิบายไฟล์, บันทึกไฟล์และข้อมูลเมตา (รวมถึง Embedding) ลงในฐานข้อมูล, และอนุญาตให้ผู้ใช้ค้นหาไฟล์ที่เกี่ยวข้องโดยใช้การค้นหาแบบ Vector Similarity

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant TrackFileProcessor
    participant LineAPI
    participant IMemoryCache
    participant OpenAIService
    participant ApplicationDbContext
    participant FileSystem
    participant SystemService

    User->>LineWebhookCommand: ส่งไฟล์ (รูปภาพ/เอกสาร)
    LineWebhookCommand->>TrackFileProcessor: เรียกใช้ ProcessLineImageAsync(evt, chatbotId, messageId, userId, replyToken, accessToken)
    TrackFileProcessor->>LineAPI: ดาวน์โหลดไฟล์จาก LINE (TryDownloadLineFileAsync)
    LineAPI-->>TrackFileProcessor: ส่งคืน File Content และ Content Type
    TrackFileProcessor->>IMemoryCache: เก็บ File Content ชั่วคราว (PendingUpload)
    TrackFileProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อมข้อความ "กรุณาระบุคำอธิบายไฟล์")
    LineWebhookCommand-->>User: แสดงข้อความ "กรุณาระบุคำอธิบายไฟล์"

    User->>LineWebhookCommand: ส่งคำอธิบายไฟล์ (ข้อความ)
    LineWebhookCommand->>TrackFileProcessor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    TrackFileProcessor->>IMemoryCache: ดึง File Content ที่รออัปโหลด (PendingUpload)
    TrackFileProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LlmKey)
    ApplicationDbContext-->>TrackFileProcessor: ส่งคืน Chatbot
    TrackFileProcessor->>OpenAIService: สร้าง Embedding สำหรับคำอธิบายไฟล์ (CallEmbeddingsAsync)
    OpenAIService-->>TrackFileProcessor: ส่งคืน Embedding
    TrackFileProcessor->>FileSystem: บันทึกไฟล์ลงดิสก์
    TrackFileProcessor->>ApplicationDbContext: บันทึกข้อมูลไฟล์ (TrackFile) พร้อม Embedding
    ApplicationDbContext-->>TrackFileProcessor: ยืนยันการบันทึก
    TrackFileProcessor->>IMemoryCache: ลบข้อมูล PendingUpload
    TrackFileProcessor->>SystemService: ขอ FullHostName
    SystemService-->>TrackFileProcessor: ส่งคืน FullHostName
    TrackFileProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อม URL ดาวน์โหลดไฟล์)
    LineWebhookCommand-->>User: แสดง URL ดาวน์โหลดไฟล์

    User->>LineWebhookCommand: ส่งข้อความ "obtain [คำค้นหา]"
    LineWebhookCommand->>TrackFileProcessor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    TrackFileProcessor->>ApplicationDbContext: ดึงข้อมูล Chatbot (LlmKey, MaximumDistance, TopKDocument)
    ApplicationDbContext-->>TrackFileProcessor: ส่งคืน Chatbot
    TrackFileProcessor->>OpenAIService: สร้าง Embedding สำหรับคำค้นหา (CallEmbeddingsAsync)
    OpenAIService-->>TrackFileProcessor: ส่งคืน Embedding
    TrackFileProcessor->>ApplicationDbContext: ค้นหาไฟล์ที่ใกล้เคียงที่สุดด้วย Vector Similarity
    ApplicationDbContext-->>TrackFileProcessor: ส่งคืน TrackFile ที่ตรงกัน
    TrackFileProcessor->>SystemService: ขอ FullHostName
    SystemService-->>TrackFileProcessor: ส่งคืน FullHostName
    TrackFileProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (พร้อม URL ดาวน์โหลดไฟล์ที่พบ)
    LineWebhookCommand-->>User: แสดง URL ดาวน์โหลดไฟล์ที่พบ
```

## แผนภาพเอนทิตี (Entity Diagram)

```mermaid
classDiagram
    class TrackFile {
        +int Id
        +string FileName
        +string FilePath
        +string ContentType
        +long FileSize
        +string UserId
        +string Description
        +float[] Embedding
        +string FileHash
        +int ChatbotId
    }
    class Chatbot {
        +int Id
        +string LlmKey
        +double MaximumDistance
        +int TopKDocument
    }
    class PendingUpload {
        +byte[] FileContent
        +string FileExtension
    }

    TrackFileProcessor ..> TrackFile : สร้างและใช้
    TrackFileProcessor ..> Chatbot : ใช้ข้อมูล
    TrackFileProcessor ..> PendingUpload : สร้างและใช้ (ใน MemoryCache)
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IMemoryCache`: ใช้สำหรับจัดเก็บเนื้อหาไฟล์ที่กำลังอัปโหลดชั่วคราว
- `ISystemService`: ใช้สำหรับดึง `FullHostName` เพื่อสร้าง URL สำหรับดาวน์โหลดไฟล์
- `ILogger<TrackFileProcessor>`: ใช้สำหรับบันทึกข้อมูล Log
- `IWebHostEnvironment`: ใช้สำหรับเข้าถึงพาธของ Content Root เพื่อบันทึกไฟล์
- `IHttpClientFactory`: ใช้สำหรับสร้าง `HttpClient` เพื่อดาวน์โหลดไฟล์จาก LINE API
- `IApplicationDbContext`: ใช้สำหรับโต้ตอบกับฐานข้อมูล (ตาราง `TrackFiles` และ `Chatbots`)
- `IOpenAiService`: ใช้สำหรับสร้าง Vector Embeddings จากคำอธิบายและคำค้นหา
