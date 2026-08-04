using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IPO.FeeService.Data
{
    public class FeeDbContextFactory : IDesignTimeDbContextFactory<FeeDbContext>
    {
        public FeeDbContext CreateDbContext(string[] args)
        {
            var connectionString = "Data Source=.;Initial Catalog=FeeServiceRecords;Integrated Security=True;TrustServerCertificate=True";
            var optionsBuilder = new DbContextOptionsBuilder<FeeDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new FeeDbContext(optionsBuilder.Options);
        }
    }
}
