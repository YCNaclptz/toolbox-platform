using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwearJar.Api.Data;
using SwearJar.Api.Models;
using Platform.Shared.Auth;

namespace SwearJar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntriesController(SwearJarDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] string? month)
    {
        var userId = User.GetUserId();
        var query = db.SwearEntries.Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(month) &&
            DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            var start = new DateTime(parsed.Year, parsed.Month, 1);
            var end = start.AddMonths(1);
            query = query.Where(e => e.Time >= start && e.Time < end);
        }

        var entries = await query
            .OrderByDescending(e => e.Time)
            .Select(e => new EntryResponse(e.Id, e.Time, e.Reason, e.Fine, e.CreatedAt))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry([FromBody] EntryRequest request)
    {
        var entry = new SwearEntry
        {
            UserId = User.GetUserId(),
            Time = request.Time,
            Reason = request.Reason,
            Fine = request.Fine
        };

        db.SwearEntries.Add(entry);
        await db.SaveChangesAsync();

        var response = new EntryResponse(entry.Id, entry.Time, entry.Reason, entry.Fine, entry.CreatedAt);
        return CreatedAtAction(nameof(GetEntries), response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEntry(int id, [FromBody] EntryRequest request)
    {
        var userId = User.GetUserId();
        var entry = await db.SwearEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return NotFound(new { message = "Entry not found" });

        entry.Time = request.Time;
        entry.Reason = request.Reason;
        entry.Fine = request.Fine;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new EntryResponse(entry.Id, entry.Time, entry.Reason, entry.Fine, entry.CreatedAt));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var userId = User.GetUserId();
        var entry = await db.SwearEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return NotFound(new { message = "Entry not found" });

        // Soft delete
        entry.IsDeleted = true;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? year)
    {
        var userId = User.GetUserId();
        var query = db.SwearEntries.Where(e => e.UserId == userId);

        if (year.HasValue)
            query = query.Where(e => e.Time.Year == year.Value);

        // Perform aggregation on the server where possible, project to an anonymous type,
        // then finalize the formatting on the client side to avoid EF translation issues.
        var raw = await query
            .GroupBy(e => new { Year = e.Time.Year, Month = e.Time.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(e => e.Fine),
                Count = g.Count()
            })
            .ToListAsync();

        var summaries = raw
            .Select(g => new MonthlySummaryResponse($"{g.Year:D4}-{g.Month:D2}", g.Total, g.Count))
            .OrderByDescending(s => s.YearMonth)
            .ToList();

        return Ok(summaries);
    }
}
