using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IReportService
{
    Task<Dictionary<string, object>> GetDashboardSummaryAsync(DateTime? from = null, DateTime? to = null);
    Task<Dictionary<string, object>> GetSalesReportAsync(DateTime from, DateTime to);
    Task<Dictionary<string, object>> GetMemberReportAsync(int memberId);
    Task<Dictionary<string, object>> GetStockReportAsync();
    Task<Dictionary<string, object>> GetLoanReportAsync(DateTime from, DateTime to);
}
