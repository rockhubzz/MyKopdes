using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IShuService
{
    Task<List<Shu>> GetAllAsync(int? year = null);
    Task<List<Shu>> GetByMemberAsync(int memberId);
    Task<Shu?> GetByIdAsync(int id);
    Task<Shu> CreateAsync(Shu shu);
    Task<decimal> GetTotalShuAsync(int? year = null);
    Task<decimal> GetMemberShuAsync(int memberId, int? year = null);
    Task<Dictionary<string, object>> CalculateShuAsync(int year);
}
