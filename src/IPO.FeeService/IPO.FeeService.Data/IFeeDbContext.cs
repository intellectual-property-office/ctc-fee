using IPO.FeeService.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace IPO.FeeService.Data
{
    public interface IFeeDbContext
    {
        DatabaseFacade Database { get; }
        DbSet<ServiceRequestType> ServiceRequestTypes { get; set; }
        DbSet<Product> Products { get; set; }
        DbSet<Fee> Fees { get; set; }
        DbSet<TaxCode> TaxCodes { get; set; }
        DbSet<QuantityRule> QuantityRules { get; set; }
        DbSet<FeesDataSeedingHistory> FeesDataSeedingHistories { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        void Dispose();
        int SaveChanges();
    }
}
