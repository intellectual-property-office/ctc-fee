using IPO.FeeService.Interfaces;
using IPO.FeeService.Models.API.FeeInformation;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPO.FeeService.Data
{
    public class FeeDbRepository : IFeeRepository
    {
        public IFeeDbContext Context { get; }
		private readonly ILogger<FeeDbRepository> _logger;
		public FeeDbRepository(IFeeDbContext context, ILogger<FeeDbRepository> logger)
        {
            Context = context;
			_logger = logger;
		}

        public virtual async Task<ServiceRequestType> GetServiceRequestDetailsAsync(ServiceRequestTypeEnum serviceRequestType)
        {
            var record = await Context.ServiceRequestTypes!
                .Include(sr => sr.ProductServiceRequestTypes)
					.ThenInclude(psrt => psrt.Product)
                    .ThenInclude(p => p!.QuantityRules)
				.Include(sr => sr.ProductServiceRequestTypes)
					.ThenInclude(psrt => psrt.Product)
					.ThenInclude(p => p!.TaxCodes)
				.Include(sr => sr.ProductServiceRequestTypes)
					.ThenInclude(psrt => psrt.Product)
					.ThenInclude(p => p!.Fees)
                .FirstOrDefaultAsync(sr => sr.Name == serviceRequestType.ToString());

            return record!;
        }

        public Task<List<ProductInformationResult>> GetProductInformation(int[] productNumbers, DateTime effectiveDate, string channel)
        {

            var productInformationRecords = Context.Products!
                    .Where(p =>
                        p.ProductNumber.HasValue && productNumbers.Contains(p.ProductNumber.Value)
                    )
                    .Include(p => p.Fees)
                    .Include(p => p.QuantityRules)
                    .Include(p => p.TaxCodes)
                    .Include(p => p.ProductServiceRequestTypes)
                        .ThenInclude(psrt => psrt.ServiceRequestType)
                    .AsEnumerable()
                    .Select(p => new
                    {
                        Product = p,
                        RecentFee = (p.Fees ?? Enumerable.Empty<Fee>())
                                .Where(f => effectiveDate >= f.EffectiveFrom && (f.EffectiveTo == null || effectiveDate <= f.EffectiveTo))
                                .OrderByDescending(f => f.EffectiveFrom)
                                .FirstOrDefault(),
                        RecentTaxCode = (p.TaxCodes ?? Enumerable.Empty<TaxCode>())
                                .Where(tc => effectiveDate >= tc.EffectiveFrom && (tc.EffectiveTo == null || effectiveDate <= tc.EffectiveTo))
                                .OrderByDescending(tc => tc.EffectiveFrom)
                                .FirstOrDefault()
                    })
                   .Select(prf =>
                   {
                       var recentExVat = prf.RecentFee?.PriceExVat ?? 0m; 
                       var recentRate = prf.RecentTaxCode?.Rate ?? 0m; 
                       var recentVat = decimal.Round(recentExVat * recentRate * 0.01m, 2);

                       return new ProductInformationResult(
                            prf.Product.ProductNumber!.Value,
                            prf.Product.Channel ?? channel,
                            prf.Product.Description ?? "",
                            prf.Product.Code ?? "",
                            recentExVat,
                            recentVat,
                            (
                                from f in (prf.Product.Fees ?? Enumerable.Empty<Fee>())
                                        .Where(f => effectiveDate >= f.EffectiveFrom
                                                    && (f.EffectiveTo == null || effectiveDate <= f.EffectiveTo))
                                        .DefaultIfEmpty()
                                from psrt in prf.Product.ProductServiceRequestTypes.DefaultIfEmpty()
                                from tax in (prf.Product.TaxCodes ?? Enumerable.Empty<TaxCode>())
                                        .Where(tc => effectiveDate >= tc.EffectiveFrom
                                                  && (tc.EffectiveTo == null || effectiveDate <= tc.EffectiveTo))
                                        .DefaultIfEmpty()
                                from qty in (prf.Product.QuantityRules ?? Enumerable.Empty<QuantityRule>())
                                        .Where(q => effectiveDate >= q.EffectiveFrom
                                                   && (q.EffectiveTo == null || effectiveDate <= q.EffectiveTo))
                                        .DefaultIfEmpty()
                                select new ProductInformationResultDetails(
                                    new ServiceRequestTypeResult(psrt.ServiceRequestType ?? new ServiceRequestType()),
                                    new ProductServiceRequestTypeResult(psrt),
                                    new FeeResult(f, tax?.Rate),
                                    new TaxCodeResult(tax),
                                    new QuantityRulesResult(qty)
                                 )
                                { }
                        ).ToList());
                   })
                   .OrderBy(pir => pir.Number).ToList();

            return Task.FromResult(productInformationRecords)!;
        }

		public void SeedDatabase(DirectoryInfo directory)
		{
			//Uncomment to support logging for deployment investigations
            //LogSeedingHistoryAndIdentities();

			var versionFiles = GetSeedDataSets(directory);
			if (!versionFiles.Any())
				return;

			using var transaction = this.Context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

			var latestDbRecord = this.Context.FeesDataSeedingHistories!
										.OrderByDescending(x => x.Version)
										.FirstOrDefault();

			_logger.LogInformation("CT-LOG: Last seeding database version: {Version}", latestDbRecord != null ? latestDbRecord.Version.ToString() : "null");
			try
			{
				foreach (var versionFile in versionFiles)
			    {
				    _logger.LogInformation("CT-LOG: Test seeding status for script version: {Version}", versionFile.Version);
				    if (latestDbRecord == null || latestDbRecord.Version < versionFile.Version)
				    {
					    _logger.LogInformation("CT-LOG: Executing seeding script version: {Version}", versionFile.Version);
					    this.Context.FeesDataSeedingHistories!
						    .Add(new FeesDataSeedingHistory()
						    {
							    CreatedOn = DateTime.UtcNow,
							    Version = versionFile.Version
						    });

					    var feeData = File.ReadAllText(versionFile.Path).Trim()
							    .Replace("\t", " ")
							    .Replace(System.Environment.NewLine, " ");

					    this.Context.Database.ExecuteSqlRaw(feeData);
				    }
			    }

				this.Context.SaveChanges();
				transaction.Commit();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CT-LOG: Error occurred while seeding Fees data");
				transaction.Rollback();
				throw;
			}
		}

		[ExcludeFromCodeCoverage]
		private void LogSeedingHistoryAndIdentities()
		{
			// Log all records in FeesDataSeedingHistories
			var histories = this.Context.FeesDataSeedingHistories!.OrderBy(x => x.Id).ToList();
			foreach (var history in histories)
			{
				_logger.LogInformation(
					"CT-LOG: FeesDataSeedingHistory: Id={Id}, Version={Version}, CreatedOn={CreatedOn}",
					history.Id, history.Version, history.CreatedOn);
			}

			// Log current identity values for main tables
			using (var command = this.Context.Database.GetDbConnection().CreateCommand())
			{
				this.Context.Database.OpenConnection();
				try
				{
					var tables = new[] {
				        "FeesDataSeedingHistories",
				        "Fees",
				        "Products",
				        "QuantityRules",
				        "ServiceRequestTypes",
				        "TaxCodes"
			        };

					foreach (var table in tables)
					{
						command.CommandText = $"SELECT IDENT_CURRENT('{table}')";
						var identityValue = command.ExecuteScalar();
						_logger.LogInformation("CT-LOG: IDENT_CURRENT('{Table}') = {IdentityValue}", table, identityValue);
					}
				}
				finally
				{
					this.Context.Database.CloseConnection();
				}
			}
		}

		protected IEnumerable<FeesDataSeedModel> GetSeedDataSets(DirectoryInfo seedDataDirectory)
        {
			_logger.LogInformation("CT-LOG: Fetching seed data script files from: {DirectoryName}", seedDataDirectory.FullName);
			var fileRegex = new Regex($"^(FeesSeedData_v)(\\d+)(.sql)$", RegexOptions.IgnoreCase);
            var versionFiles = seedDataDirectory
                               .GetFiles(@"*.*", SearchOption.TopDirectoryOnly)
                               .Where(o => fileRegex.IsMatch(o.Name) && o.Length > 0)
                               .Select(o => new
                               {
                                   version = Int32.Parse(o.Name.Split("_v").Last().Split('.').First())
                                                  ,
                                   isEmpty = (o.Length == 0)
                                                  ,
                                   path = o.FullName
                               })
                               .Where(o => !o.isEmpty)
                               .OrderBy(o => o.version);

            return versionFiles.Where(o => o != null).Select(o => new FeesDataSeedModel(o.version, o.path));
        }

        public DirectoryInfo GetSeedDataDirectory(string solutionPath)
        {
            DirectoryInfo seedDataDirectory;
#if DEBUG
            var contentRootPathSegments = solutionPath.Split("\\");
            var solutionName = contentRootPathSegments[(contentRootPathSegments.Length - 2)];
            seedDataDirectory = new DirectoryInfo($"{solutionPath}\\..\\{solutionName}.Data\\SeedData");
#else
            seedDataDirectory = new DirectoryInfo(Path.Join(solutionPath, "SeedData"));
#endif

            return seedDataDirectory.Exists ?
                   seedDataDirectory :
                   throw new DirectoryNotFoundException($"The directory for the Fees seed data does not exist. path: {solutionPath}");
        }
    }
}
