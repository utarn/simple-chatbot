# สคริปต์ติดตั้งสภาพแวดล้อมการพัฒนา

สคริปต์ PowerShell นี้จะติดตั้งเครื่องมือพัฒนาที่จำเป็นสำหรับระบบ Windows โดยอัตโนมัติ รวมถึงคำแนะนำการใช้งานโปรเจค Chatbot API

## สารบัญ

- [เครื่องมือที่ติดตั้ง](#เครื่องมือที่ติดตั้ง)
- [ความต้องการระบบ](#ความต้องการระบบ)
- [วิธีใช้งาน](#วิธีใช้งาน)
- [สิทธิ์ผู้ดูแลระบบ](#สิทธิ์ผู้ดูแลระบบ)
- [วิธีการทำงาน](#วิธีการทำงาน)
- [หมายเหตุ](#หมายเหตุ)
- [การสร้าง EF Core Migrations และ Database Update](#การสร้าง-ef-core-migrations-และ-database-update)
- [การ Build และการ Run โปรเจค](#การ-build-และการ-run-โปรเจค)
- [การ Deploy ผ่าน Docker](#การ-deploy-ผ่าน-docker)
- [การสมัครสมาชิก ngrok และการตั้งค่าคำสั่งในการรัน](#การสมัครสมาชิก-ngrok-และการตั้งค่าคำสั่งในการรัน)

## เครื่องมือที่ติดตั้ง

1. .NET 8 SDK
2. Visual Studio Code (พร้อมส่วนขยายจำเป็น)
3. Git
4. Windows Terminal
5. PowerShell 7
6. PostgreSQL 17
7. Docker Desktop
8. Ngrok

## ความต้องการระบบ

- ระบบปฏิบัติการ Windows
- PowerShell 5.0 หรือสูงกว่า
- การเชื่อมต่ออินเทอร์เน็ต

## วิธีใช้งาน

### เปิดใช้งานด้วยการตั้งค่าเริ่มต้น
```powershell
.\setup-dev-environment.ps1
```

### เปิดใช้งานโดยไม่ตรวจสอบสิทธิ์ผู้ดูแลระบบ
```powershell
.\setup-dev-environment.ps1 -SkipAdminCheck
```

### บังคับติดตั้งโดยไม่มีการถาม
```powershell
.\setup-dev-environment.ps1 -Force
```

## สิทธิ์ผู้ดูแลระบบ

เพื่อผลลัพธ์ที่ดีที่สุด ควรเปิดใช้งานสคริปต์นี้ในฐานะผู้ดูแลระบบ:
1. คลิกขวาที่ PowerShell
2. เลือก "Run as Administrator"
3. ไปยังโฟลเดอร์ที่มีสคริปต์
4. เปิดใช้งานสคริปต์

หากไม่ได้เปิดใช้งานในฐานะผู้ดูแลระบบ สคริปต์จะถามให้ดำเนินการต่อหรือยกเลิกการติดตั้ง

## วิธีการทำงาน

สคริปต์จะตรวจสอบว่าติดตั้งเครื่องมือแต่ละอย่างแล้วหรือไม่ก่อนที่จะพยายามติดตั้ง หากพบเครื่องมือแล้วจะข้ามการติดตั้งนั้น

สำหรับการติดตั้ง สคริปต์จะพยายามใช้ตัวจัดการแพ็คเกจตามลำดับนี้:
1. winget (ตัวจัดการแพ็คเกจ Windows)
2. Chocolatey
3. ดาวน์โหลดโดยตรงจากแหล่งที่มา官方

หากไม่มีตัวจัดการแพ็คเกจใดๆ จะใช้การดาวน์โหลดโดยตรง

## หมายเหตุ

- สคริปต์จะดาวน์โหลดตัวติดตั้งไปยังโฟลเดอร์ TEMP ของระบบและลบออกหลังการติดตั้งเสร็จสิ้น
- การติดตั้งบางอย่างอาจต้องรีสตาร์ทเครื่องเพื่อให้เสร็จสมบูรณ์
- การติดตั้ง PostgreSQL จะต้องตั้งรหัสผ่านระหว่างกระบวนการติดตั้งหากใช้การดาวน์โหลดโดยตรง
## การสร้าง EF Core Migrations และ Database Update

โปรเจคนี้ใช้ Entity Framework Core กับฐานข้อมูล PostgreSQL สำหรับการจัดการฐานข้อมูล

### การสร้าง Migration ใหม่

เมื่อมีการเปลี่ยนแปลงใน Entity หรือ Database Schema ให้สร้าง Migration ดังนี้:

```bash
dotnet ef migrations add <MigrationName> --project src/Infrastructure/Infrastructure.csproj --startup-project src/Web/Web.csproj
```

ตัวอย่าง:
```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure/Infrastructure.csproj --startup-project src/Web/Web.csproj
```

### การอัปเดตฐานข้อมูล

หลังจากสร้าง Migration แล้ว ให้อัปเดตฐานข้อมูล:

```bash
dotnet ef database update --project src/Web/Web.csproj --startup-project src/Web/Web.csproj
```

สำหรับการทำงานกับฐานข้อมูลใน Docker ให้ตรวจสอบว่าฐานข้อมูลกำลังรันอยู่ก่อน

## การ Build และการ Run โปรเจค

### การ Build โปรเจค

เพื่อ Build โปรเจคในโหมด Debug:

```bash
dotnet build
```

หรือสำหรับโหมด Release:

```bash
dotnet build --configuration Release
```

### การ Run โปรเจค

เพื่อรันโปรเจคในโหมดพัฒนา:

```bash
dotnet run --project src/Web/Web.csproj
```

โดยปกติแอปพลิเคชันจะรันที่ `http://localhost:5000` หรือตามที่กำหนดใน `launchSettings.json`

## การ Deploy ผ่าน Docker

โปรเจคนี้มี Dockerfile และ docker-compose.yml สำหรับการ Deploy ผ่าน Docker

### การ Build และรันด้วย Docker Compose

```bash
docker-compose up --build
```

คำสั่งนี้จะ:
- Build image ของแอปพลิเคชัน
- รันฐานข้อมูล PostgreSQL ด้วย pgvector
- รันแอปพลิเคชันที่พอร์ต 8003

### การหยุดการทำงาน

```bash
docker-compose down
```

### การลบ Volume ของฐานข้อมูล (เมื่อต้องการเริ่มใหม่)

```bash
docker-compose down -v
```

## การสมัครสมาชิก ngrok และการตั้งค่าคำสั่งในการรัน

ngrok ใช้สำหรับสร้าง tunnel ให้กับ localhost เพื่อให้เข้าถึงได้จากภายนอก

### การสมัครสมาชิก ngrok

1. เข้าเว็บไซต์ [ngrok.com](https://ngrok.com)
2. สมัครสมาชิกฟรี
3. ดาวน์โหลดและติดตั้ง ngrok

### การตั้งค่า Authentication Token

หลังจากติดตั้ง ให้ตั้งค่า token:

```bash
ngrok config add-authtoken <YOUR_AUTH_TOKEN>
```

### การรัน ngrok สำหรับโปรเจคนี้

เมื่อแอปพลิเคชันรันอยู่ที่พอร์ต 8003 (จาก Docker):

```bash
ngrok http 8003
```

ngrok จะให้ URL สาธารณะ เช่น `https://abcd1234.ngrok.io` ที่สามารถเข้าถึงแอปพลิเคชันได้

### คำสั่งที่มีประโยชน์

- ดูสถานะ: `ngrok status`
- หยุด tunnel: `Ctrl+C`
- ดู log: เมื่อรันคำสั่งจะแสดง log ใน terminal