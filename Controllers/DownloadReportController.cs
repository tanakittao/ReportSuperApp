using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Controllers
{
    /* =======================================================
       1. DTO (Data Transfer Object) - สำหรับส่งข้อมูลกลับไปที่ Dashboard (JSON)
       ======================================================= */
    public class SubItemDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
        public int Target { get; set; }
    }

    public class RegionReportDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
        public int Target { get; set; }
        public string Product { get; set; }
        public List<SubItemDto> SubItems { get; set; } = new List<SubItemDto>();
    }

    /* =======================================================
       คลาส DTO สำหรับรับข้อมูลจำลองจากไฟล์ appsettings.json
       ======================================================= */
    public class RegionConfigDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
        public string Product { get; set; }
        public List<SubItemDto> SubItems { get; set; } = new List<SubItemDto>();
    }

    /* =======================================================
       2. API Controller - จุดรับ Request จาก Dashboard หน้าบ้าน
       ======================================================= */
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadReportController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // รับค่า IConfiguration เพื่อใช้อ่านข้อมูลจาก appsettings.json
        public DownloadReportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Endpoint: GET /api/DownloadReport/regional-summary
        [HttpGet("regional-summary")]
        public IActionResult GetRegionalSummary(
            [FromQuery] string regionId = "ALL", 
            [FromQuery] string product = "ALL")
        {
            try
            {
                // ดึงค่า Config (เป้าหมาย, ข้อมูลเขต) จาก appsettings.json
                var targetPercentage = _configuration.GetValue<double>("TargetConfiguration:TargetPercentage", 100.0);
                var regionConfigs = _configuration.GetSection("TargetConfiguration:Regions").Get<List<RegionConfigDto>>() ?? new List<RegionConfigDto>();

                // แปลง List เป็น Queryable เพื่อทำการ Where กรองข้อมูล
                var query = regionConfigs.AsQueryable();

                // กรองข้อมูลตาม Parameters ที่ส่งมาจากหน้า Dashboard (LINQ)
                if (!string.IsNullOrEmpty(regionId) && regionId != "ALL")
                    query = query.Where(x => x.Id == regionId);

                if (!string.IsNullOrEmpty(product) && product != "ALL")
                    query = query.Where(x => x.Product == product);
                    
                /* ============================================================
                   ✅ ใช้ .AsEnumerable() ก่อน .Select() เพื่อป้องกัน Error CS0834 
                   และให้ระบบคำนวณเป้าหมาย (Target) แบบไดนามิกในหน่วยความจำ
                   ============================================================ */
                var result = query.AsEnumerable().Select(x => {
                    
                    // คำนวณเป้าหมายเฉพาะเขตหลัก (Dynamic Target)
                    int dynamicTarget = (int)Math.Round(x.StaffCount * (targetPercentage / 100.0));

                    // ดึงข้อมูล SubItems ออกมา (ถ้ามี) และคำนวณเป้าหมายจากเปอร์เซ็นต์ด้วย
                    var mappedSubItems = x.SubItems?.Select(sub => new SubItemDto {
                        Id = sub.Id,
                        Name = sub.Name,
                        StaffCount = sub.StaffCount,
                        Downloads = sub.Downloads,
                        Target = (int)Math.Round(sub.StaffCount * (targetPercentage / 100.0))
                    }).ToList() ?? new List<SubItemDto>();

                    return new RegionReportDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StaffCount = x.StaffCount,
                        Product = x.Product,
                        Downloads = x.Downloads,
                        Target = dynamicTarget,
                        SubItems = mappedSubItems
                    };
                })
                .OrderByDescending(x => x.Downloads) // เรียงลำดับจากยอดดาวน์โหลดรวมมากไปน้อย
                .ToList();

                // ส่งข้อมูลกลับเป็น JSON Status 200 OK
                return Ok(result);
            }
            catch (Exception ex)
            {
                // กรณีเกิด Error ส่ง Status 500 กลับไปพร้อมข้อความแจ้งเตือน
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูล", error = ex.Message });
            }
        }
    }
}