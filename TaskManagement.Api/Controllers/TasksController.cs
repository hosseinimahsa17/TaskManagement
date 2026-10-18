using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        // تزریق وابستگی (Dependency Injection) برای دسترسی به دیتابیس
        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("streak")]
        public async Task<ActionResult<int>> GetStreak()
        {
            var tasks = await _context.Tasks.ToListAsync();
            if (!tasks.Any()) return Ok(0);

            // استخراج روزهای یکتایی که تسک ثبت شده (بر اساس تاریخ UTC)
            var activeDays = tasks.Select(t => t.CreatedAt.Date).Distinct().OrderByDescending(d => d).ToList();

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // اگه امروز یا دیروز تسک ثبت نشده باشه، زنجیره شکسته شده
            if (activeDays[0] != today && activeDays[0] != yesterday)
            {
                return Ok(0);
            }

            int streak = 1;
            var currentDate = activeDays[0];

            foreach (var day in activeDays.Skip(1))
            {
                if (day == currentDate.AddDays(-1))
                {
                    streak++;
                    currentDate = day;
                }
                else
                {
                    break; // زنجیره شکست
                }
            }

            return Ok(streak);
        }
        // 1. GET: api/tasks (دریافت همه تسک‌ها)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }

        [HttpGet("daily-summary")]
        public async Task<ActionResult<IEnumerable<DailyTaskSummaryDto>>> GetDailySummary()
        {
            var tasks = await _context.Tasks.ToListAsync();

            var summary = tasks
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new DailyTaskSummaryDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(t => t.IsCompleted)
                })
                .OrderByDescending(s => s.Date)
                .ToList();

            return Ok(summary);
        }


        // 2. GET: api/tasks/5 (دریافت یک تسک خاص با آیدی)
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound(); // اگه پیدا نشد ارور 404 بده
            }

            return task;
        }

        // 3. POST: api/tasks (ساخت یک تسک جدید)
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // بعد از ساخت، آدرس تسک جدید رو برمی‌گردونه
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        // 4. PUT: api/tasks/5 (ویرایش یک تسک)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem task)
        {
            if (id != task.Id)
            {
                return BadRequest(); // اگه آیدی‌ها یکی نبود ارور 400 بده
            }

            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // ارور 204 (یعنی موفق بود ولی چیزی برنمی‌گردونه)
        }

        // 5. DELETE: api/tasks/5 (حذف یک تسک)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // متد کمکی برای چک کردن وجود تسک
        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}