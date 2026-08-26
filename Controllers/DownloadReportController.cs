using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseDashboard.Controllers
{
    /* =======================================================
       1. DTO (Data Transfer Object) - สำหรับส่งข้อมูลกลับไปที่ Dashboard (JSON)
       ======================================================= */
    public class RegionReportDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
        public int Target { get; set; }
        public string Platform { get; set; }
        public string Product { get; set; }
        public string Period { get; set; }
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
        public string Platform { get; set; }
        public string Product { get; set; }
        public string Period { get; set; }
    }

    /* =======================================================
       2. API Controller - จุดรับ Request จาก Dashboard หน้าบ้าน
       ======================================================= */
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadReportController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // รับแค่ IConfiguration อย่างเดียว (ลบฐานข้อมูลออกไปแล้ว)
        public DownloadReportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Endpoint: GET /api/DownloadReport/regional-summary
        [HttpGet("regional-summary")]
        public IActionResult GetRegionalSummary(
            [FromQuery] string regionId = "ALL", 
            [FromQuery] string product = "ALL", 
            [FromQuery] string period = "2026-Q3", 
            [FromQuery] string platform = "ALL")
        {
            try
            {
                // ดึงค่า Config (เป้าหมาย, ยอดดาวน์โหลด, บุคลากร) จาก appsettings.json
                var targetPercentage = _configuration.GetValue<double>("TargetConfiguration:TargetPercentage", 100.0);
                var regionConfigs = _configuration.GetSection("TargetConfiguration:Regions").Get<List<RegionConfigDto>>() ?? new List<RegionConfigDto>();

                // แปลง List เป็น Queryable เพื่อทำการ Where กรองข้อมูล
                var query = regionConfigs.AsQueryable();

                // กรองข้อมูลตาม Parameters ที่ส่งมาจากหน้า Dashboard (LINQ)
                if (!string.IsNullOrEmpty(regionId) && regionId != "ALL")
                    query = query.Where(x => x.Id == regionId);

                if (!string.IsNullOrEmpty(product) && product != "ALL")
                    query = query.Where(x => x.Product == product);
                    
                if (!string.IsNullOrEmpty(period) && period != "ALL")
                    query = query.Where(x => x.Period == period);

                if (!string.IsNullOrEmpty(platform) && platform != "ALL")
                    query = query.Where(x => x.Platform == platform);

                /* ============================================================
                   ✅ วิธีแก้ Error CS0834 : เติม .AsEnumerable() ลงไปก่อน .Select()
                   เพื่อให้ระบบดึงข้อมูลมาไว้ใน Memory ก่อน แล้วค่อยคำนวณแบบใส่ปีกกา { }
                   ============================================================ */
                var result = query.AsEnumerable().Select(x => {
                    
                    // คำนวณเป้าหมายใหม่ (Dynamic Target) = จำนวนบุคลากร * (เปอร์เซ็นต์ / 100)
                    int dynamicTarget = (int)Math.Round(x.StaffCount * (targetPercentage / 100.0));

                    return new RegionReportDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        StaffCount = x.StaffCount,
                        Platform = x.Platform,
                        Product = x.Product,
                        Period = x.Period,
                        Downloads = x.Downloads,
                        Target = dynamicTarget
                    };
                })
                .OrderByDescending(x => x.Downloads) // เรียงจากยอดดาวน์โหลดมากไปน้อย
                .ToList();

                // ส่งข้อมูลกลับเป็น JSON Status 200 OK
                return Ok(result);
            }
            catch (Exception ex)
            {
                // กรณีเกิด Error ส่ง Status 500 กลับไป
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูล", error = ex.Message });
            }
        }
    }
}