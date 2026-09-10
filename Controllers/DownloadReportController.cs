using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDashboard.Controllers
{
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
        public List<SubItemDto> SubItems { get; set; } = new List<SubItemDto>();
    }

    public class SubItemConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
    }

    public class RegionConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StaffCount { get; set; }
        public int Downloads { get; set; }
        public List<SubItemConfig> SubItems { get; set; } = new List<SubItemConfig>();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DownloadReportController : ControllerBase
    {
        // ใช้งาน IConfiguration เพื่อเข้าถึงไฟล์ appsettings.json
        private readonly IConfiguration _configuration;

        public DownloadReportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("regional-summary")]
        public IActionResult GetRegionalSummary([FromQuery] string regionId = "ALL")
        {
            try
            {
                // 1. อ่านค่าเปอร์เซ็นต์เป้าหมายจาก TargetConfiguration:TargetPercentage
                var targetPercentage = _configuration.GetValue<double>("TargetConfiguration:TargetPercentage", 70.0);

                // 2. ดึงข้อมูล Regions ทั้งหมดจากไฟล์ appsettings.json (รวมถึง SubItems)
                var regionConfigs = _configuration.GetSection("TargetConfiguration:Regions").Get<List<RegionConfig>>() ?? new List<RegionConfig>();

                var query = regionConfigs.AsQueryable();

                // กรองข้อมูลตามเขต (ถ้าหน้าเว็บส่งพารามิเตอร์มา)
                if (!string.IsNullOrEmpty(regionId) && regionId != "ALL")
                    query = query.Where(x => x.Id == regionId);

                // 3. จัดรูปข้อมูลและคำนวณเป้าหมาย (Target) ให้สอดคล้องกับจำนวนบุคลากร
                var result = query.AsEnumerable().Select(x => {
                    
                    // คำนวณเป้าหมายของเขตหลัก
                    int mainTarget = (int)Math.Round(x.StaffCount * (targetPercentage / 100.0));

                    // นำข้อมูลหน่วยงานย่อยมาคำนวณเป้าหมายเช่นเดียวกัน (ถ้ามี)
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
                        Downloads = x.Downloads,
                        Target = mainTarget,
                        SubItems = mappedSubItems
                    };
                })
                .OrderByDescending(x => x.Downloads) 
                .ToList();

                // ส่งข้อมูล JSON กลับไปที่หน้าเว็บ (Status 200)
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดในการดึงข้อมูลจาก Configuration File", error = ex.Message });
            }
        }

        [HttpGet("last-update")]
        public IActionResult GetLastUpdateDate()
        {
            // ดึงข้อมูลวันที่จาก appsettings.json ถ้าไม่มีให้แสดงค่าเริ่มต้น
            var lastUpdate = _configuration.GetValue<string>("TargetConfiguration:LastUpdateDate", "ไม่ได้ระบุวันที่");
            return Ok(new { date = lastUpdate });
        }
    }
}