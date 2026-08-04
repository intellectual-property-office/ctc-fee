using IPO.FeeService.Models.Data;
using IPO.FeeService.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace IPO.FeeService.Data
{
    public class FeeDbContext : DbContext, IFeeDbContext
    {
        public FeeDbContext() { }
        public FeeDbContext(DbContextOptions options) : base(options)
        {
        }

		public DbSet<ServiceRequestType> ServiceRequestTypes { get; set; } = null!;
		public DbSet<Product> Products { get; set; } = null!;
		public DbSet<Fee> Fees { get; set; } = null!;
		public DbSet<TaxCode> TaxCodes { get; set; } = null!;
		public DbSet<QuantityRule> QuantityRules { get; set; } = null!;

		public DbSet<FeesDataSeedingHistory> FeesDataSeedingHistories { get; set; } = null!;

		public DbSet<ProductServiceRequestType> ProductServiceRequestTypes { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<ProductServiceRequestType>(entity =>
			{
				entity.ToTable("ProductServiceRequestType");

				entity.HasKey(e => new { e.ProductsId, e.ServiceRequestTypesId });
				entity.Property(e => e.ProductsId)
					  .IsRequired();
				entity.Property(e => e.ServiceRequestTypesId)
					  .IsRequired();

				entity.Property(e => e.ProductSequence)
					  .HasDefaultValue(1)
					  .IsRequired();

				entity.HasOne(e => e.Product)
					  .WithMany(p => p.ProductServiceRequestTypes)
					  .HasForeignKey(e => e.ProductsId)
					  .IsRequired()
					  .OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(e => e.ServiceRequestType)
					  .WithMany(s => s.ProductServiceRequestTypes)
					  .HasForeignKey(e => e.ServiceRequestTypesId)
					  .IsRequired()
					  .OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<Fee>()
						.Property(f => f.Rule)
						.HasDefaultValue(Settings.NoFeeRuleValue);
		}
	}
}
