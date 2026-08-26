using EnterpriseDashboard.Controllers;
// ลบบรรทัด Microsoft.EntityFrameworkCore ออก เนื่องจากไม่ได้ใช้ In-Memory DB แล้ว

var builder = WebApplication.CreateBuilder(args);

// 1. เพิ่มบริการ Controllers และ Swagger สำหรับ API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ลบการตั้งค่า In-Memory Database (builder.Services.AddDbContext) ออกทั้งหมด

var app = builder.Build();

// 2. กำหนด Pipeline การทำงานของ HTTP Request
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // ดู API Docs ได้ที่ /swagger
}

app.UseHttpsRedirection();

// ========================================================
// 3. การแสดงผล Frontend (เสิร์ฟไฟล์ HTML จากโฟลเดอร์ wwwroot)
// ========================================================
app.UseDefaultFiles(); // อนุญาตให้ดึง index.html มาแสดงเป็นหน้าแรกโดยอัตโนมัติ
app.UseStaticFiles();  // เปิดใช้งานการเสิร์ฟไฟล์ Static (HTML, CSS, JS)

app.UseAuthorization();
app.MapControllers(); // เปิดระบบ Routing API (/api/...)

// ลบส่วนที่ใช้ Seed Data (app.Services.CreateScope()) ลง Database ทิ้งทั้งหมด 

app.Run();