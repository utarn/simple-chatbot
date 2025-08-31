# Plugin Auto-Registration System

## Overview

ระบบ auto-registration สำหรับ plugins ใน ChatBot API ได้ถูกปรับปรุงเพื่อให้สามารถค้นหาและลงทะเบียน processors ทั้งหมดโดยอัตโนมัติ โดยใช้ **class name pattern** และ **properties** แทน attributes

## การเปลี่ยนแปลงหลัก

### 1. Class Name Pattern Discovery

ระบบจะค้นหา classes อัตโนมัติด้วยเงื่อนไข:
- Class name ลงท้ายด้วย `*Processor`
- Implement `ILineMessageProcessor` หรือ `IFacebookMessengerProcessor`
- ไม่เป็น interface หรือ abstract class

### 2. Properties-Based Configuration

ใช้ properties `Name` และ `Description` แทน attributes:
- `Name` - ชื่อ plugin (ต้องมี constant ใน Systems.cs)
- `Description` - คำอธิบาย plugin ที่แสดงใน UI

### 3. PluginDiscoveryService

สร้าง service สำหรับค้นหา processors อัตโนมัติ:

```csharp
public interface IPluginDiscoveryService
{
    IReadOnlyDictionary<string, PluginInfo> GetAvailablePlugins();
    PluginInfo? GetPluginInfo(string pluginName);
}

public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Type ProcessorType { get; set; } = null!;
}
```

### 4. อัปเดต GetPluginByChatBotQuery

แก้ไขให้ใช้ discovered plugins แทน hardcoded `Systems.Plugins`:

```csharp
// เปลี่ยนจาก
foreach (var (key, value) in Systems.Plugins)

// เป็น
var availablePlugins = _pluginDiscoveryService.GetAvailablePlugins();
foreach (var (pluginName, pluginInfo) in availablePlugins)
```

## วิธีการเพิ่ม Plugin ใหม่

### ขั้นตอนที่ 1: สร้าง Processor Class

```csharp
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Constants;

namespace ChatbotApi.Infrastructure.Processors.MyNewProcessor
{
    public class MyNewProcessor : ILineMessageProcessor  // Class name ต้องลงท้ายด้วย "Processor"
    {
        public string Name => Systems.MyNewPlugin; // ต้องมี constant ใน Systems.cs
        public string Description => "คำอธิบาย plugin ใหม่ (Line)"; // แสดงใน UI

        // Implementation...
    }
}
```

### ขั้นตอนที่ 2: เพิ่ม Constant ใน Systems.cs

```csharp
public abstract class Systems
{
    // เพิ่ม constant ใหม่
    public const string MyNewPlugin = "MyNewPlugin";
    
    // ไม่ต้องแก้ไข Plugins dictionary อีกต่อไป!
}
```

### ขั้นตอนที่ 3: ทดสอบ

1. Build และ run application
2. Plugin จะถูกค้นหาและลงทะเบียนอัตโนมัติ
3. ตรวจสอบใน chatbot management UI ว่า plugin ใหม่ปรากฏในรายการ

## Processors ที่ได้รับการอัปเดตแล้ว

รายการ processors ที่ได้อัปเดตให้ใช้ `Description` property:

1. **EchoProcessor** - "Echo Bot (Line): ตอบกลับข้อความพร้อมวันที่และเวลา"
2. **GoldReportProcessor** - "รายงานราคาทองคำ (Line)"
3. **ReadCodeProcessor** - "อ่าน QR Code และ Barcode (Line)"
4. **UserDefinedProcessor** - "เปิดการใช้งาน Line Flex หรือ Custom JSON (Line, GoogleChat)"
5. **CheckCheatOnlineProcessor** - "ตรวจสอบการโกงออนไลน์ (Line)"
6. **FormT1Processor** - "สร้างใบลาป่วย ลาพักผ่อน ลาคลอดบุตร (Line)"
7. **LLamaPassportProcessor** - "Llama Passport (Line)"
8. **ReceiptProcessor** - "สร้างใบเสร็จรับเงิน (Line)"
9. **TrackFileProcessor** - "จัดการไฟล์แนับ (Line)"
10. **ReadImageProcessor** - "วิเคราะห์รูปภาพ (Line)"
11. **ExampleLineEmailProcessor** - "สรุปเนื้อหาอีเมล (Line)"

## การทำงานของระบบ

1. **Application Startup**: `PluginDiscoveryService` จะ scan assemblies ทั้งหมดเพื่อหา classes ที่:
   - Class name ลงท้ายด้วย "Processor"
   - Implement `ILineMessageProcessor` หรือ `IFacebookMessengerProcessor`
2. **Plugin Discovery**: สร้าง instance เพื่อดึง `Name` และ `Description` properties
3. **Registration**: เก็บข้อมูลใน dictionary พร้อมใช้งาน
4. **Runtime**: `GetPluginByChatBotQuery` ใช้ discovered plugins แทน hardcoded list

## ข้อดี

- **ไม่ต้องใช้ Attributes**: ใช้ class name pattern และ properties เท่านั้น
- **Automatic Discovery**: ระบบจะหา processors ใหม่อัตโนมัติ
- **Maintainable**: ลดความซับซ้อนในการ maintain plugin list
- **Scalable**: รองรับการเพิ่ม plugins จำนวนมากโดยไม่ต้องแก้ไขหลายที่
- **Type Safety**: ยังคงใช้ constants ใน Systems.cs เพื่อความปลอดภัย
- **Flexible**: รองรับทั้ง Line และ Facebook Messenger processors

## เงื่อนไขสำหรับ Auto-Discovery

1. **Class name**: ต้องลงท้ายด้วย "Processor" (เช่น MyNewProcessor, EmailProcessor)
2. **Interface**: ต้อง implement `ILineMessageProcessor` หรือ `IFacebookMessengerProcessor`
3. **Properties**: ต้องมี `Name` และ `Description` properties
4. **Constructor**: ต้องมี parameterless constructor หรือใช้ DI

## หมายเหตุ

- ยังคงต้องเพิ่ม constant ใน `Systems.cs` เพื่อ reference ใน `Name` property
- `PluginDiscoveryService` ถูกลงทะเบียนเป็น Singleton เพื่อ performance
- การค้นหา plugins ทำงานเพียงครั้งเดียวตอน application startup

## การ Troubleshooting

1. **Plugin ไม่ปรากฏใน UI**: ตรวจสอบว่า class name ลงท้ายด้วย "Processor" และ implement interface ถูกต้อง
2. **Build Error**: ตรวจสอบ using statements และ interface implementations
3. **Runtime Error**: ตรวจสอบ DI registration, constructor, และ constant ใน Systems.cs
4. **Missing Description**: ตรวจสอบว่ามี `Description` property และ return ค่าที่ถูกต้อง