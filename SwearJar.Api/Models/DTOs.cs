namespace SwearJar.Api.Models;

public record EntryRequest(DateTime Time, string Reason, int Fine);
public record EntryResponse(int Id, DateTime Time, string Reason, int Fine, DateTime CreatedAt);
public record MonthlySummaryResponse(string YearMonth, int Total, int Count);
