# UserDefinedProcessor

## วัตถุประสงค์ (Purpose)
`UserDefinedProcessor` มีหน้าที่ประมวลผลข้อความจากผู้ใช้เพื่อตรวจสอบว่าข้อความนั้นตรงกับคำสั่งที่ผู้ใช้กำหนดไว้ล่วงหน้า (User-Defined Commands) ซึ่งถูกเก็บอยู่ในตาราง `FlexMessages` หรือไม่ หากพบคำสั่งที่ตรงกัน จะส่งคืนค่า JSON ที่เกี่ยวข้องกับคำสั่งนั้นกลับไป หากไม่พบ จะส่งคืนสถานะว่าไม่พบข้อมูล

## แผนภาพลำดับเหตุการณ์ (Sequence Diagram)

```mermaid
sequenceDiagram
    participant User
    participant LineWebhookCommand
    participant UserDefinedProcessor
    participant ApplicationDbContext

    User->>LineWebhookCommand: ส่งข้อความ (Line Message)
    LineWebhookCommand->>UserDefinedProcessor: เรียกใช้ ProcessLineAsync(evt, chatbotId, message, userId, replyToken)
    UserDefinedProcessor->>ApplicationDbContext: ค้นหา FlexMessages ที่ตรงกับ chatbotId และ message.Key
    ApplicationDbContext-->>UserDefinedProcessor: ส่งคืน FlexMessage ที่ตรงกัน (ถ้ามี)
    alt พบ FlexMessage ที่ตรงกัน
        UserDefinedProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (Status 201 พร้อม Raw JSON Value)
        LineWebhookCommand-->>User: ส่งข้อความตอบกลับตาม JSON Value
    else ไม่พบ FlexMessage ที่ตรงกัน
        UserDefinedProcessor-->>LineWebhookCommand: ส่งคืน LineReplyStatus (Status 404)
        LineWebhookCommand-->>User: (ไม่มีการตอบกลับจาก Processor นี้)
    end
```

## แผนภาพเอนทิตี (Entity Diagram)

```mermaid
classDiagram
    class Chatbot {
        +int Id
    }
    class FlexMessage {
        +int Id
        +int ChatbotId
        +string Key
        +string JsonValue
        +string Type
        +int Order
    }

    UserDefinedProcessor ..> Chatbot : ใช้ข้อมูล
    UserDefinedProcessor ..> FlexMessage : ใช้ข้อมูล
    Chatbot "1" -- "*" FlexMessage : มี
```

## บริการที่เกี่ยวข้อง (Related Services)
- `IApplicationDbContext`: ใช้สำหรับเข้าถึงข้อมูลในฐานข้อมูล โดยเฉพาะตาราง `FlexMessages` เพื่อค้นหาคำสั่งที่ผู้ใช้กำหนด
