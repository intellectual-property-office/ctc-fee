using AwesomeAssertions;
using IPO.FeeService.Data;
using IPO.FeeService.Models.Constants;
using IPO.FeeService.Models.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceRequestType = IPO.FeeService.Models.Data.ServiceRequestType;

namespace IPO.FeeService.UnitTests.Data
{
    [TestClass]
    public class FeeDBRepositoryTests
    {
        private readonly FeeDbRepository _feeDbRepository;
        private readonly DbConnection _connection;
		private readonly Mock<ILogger<FeeDbRepository>> _mockLogger;

		public FeeDBRepositoryTests()
        {
			_mockLogger = new Mock<ILogger<FeeDbRepository>>();
			_connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            this._feeDbRepository = new FeeDbRepository(
                                            new FeeDbContext(
                                            new DbContextOptionsBuilder<FeeDbContext>()
                                            .UseSqlite(_connection)
                                            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                                            .Options
                                            ),
											_mockLogger.Object);

            // Create the schema and seed some data
            if (this._feeDbRepository.Context.Database.EnsureCreated())
            {
                Helper.SetupServiceRequestTypesInRepo(this._feeDbRepository);
                var count = this._feeDbRepository.Context.ServiceRequestTypes!.Count();
            }
        }
        public void Dispose() => _connection.Dispose();

        [TestCleanup]
        public void TearDown()
        {
            Dispose();
        }

        [DataRow(ServiceRequestTypeEnum.PatentApplicationAndSearch)]
        [DataRow(ServiceRequestTypeEnum.CertifiedOfficeCopies)]
        [TestMethod]
        public async Task GetServiceRequestDetailsAsyncReturnsExpectedServiceRequestRecord(ServiceRequestTypeEnum serviceRequestType)
        {
            //Arrange

            //Act
            var serviceRequestResult = await _feeDbRepository.GetServiceRequestDetailsAsync(serviceRequestType);

            //Assert
            this._feeDbRepository.Context.ServiceRequestTypes.Should().HaveCount(Enum.GetNames(typeof(ServiceRequestTypeEnum)).Length);
            serviceRequestResult.Should().NotBeNull();
            serviceRequestResult.Name.Should().Match(serviceRequestType.ToString());
        }

        #region Fees data import seed data tests

        [TestMethod]
        public void SeedDatabaseWhenNoVersionFileExistReturns()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                , "Resources"
                , "emptyfolder");

            var directoryInfo = new DirectoryInfo(path);

            var serviceRequestCount = this._feeDbRepository.Context.ServiceRequestTypes!.Count();

            // Act
            this._feeDbRepository.SeedDatabase(directoryInfo);

            // Assert
            this._feeDbRepository.Context.FeesDataSeedingHistories.Should().BeNullOrEmpty();
            this._feeDbRepository.Context.ServiceRequestTypes.Should().HaveCount(serviceRequestCount);
        }

        [TestMethod]
        public void SeedDatabaseWhenOlderOrExistingVersionFilesExistThenNoDatabaseChangesHappen()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                , "Resources");

            var directoryInfo = new DirectoryInfo(path);

            var serviceRequestCount = this._feeDbRepository.Context.ServiceRequestTypes!.Count();

            var feesDataSeedingHistoryRecord = new FeesDataSeedingHistory()
            {
                CreatedOn = DateTime.Now,
                Version = 5
            };

            this._feeDbRepository.Context.FeesDataSeedingHistories!.Add(feesDataSeedingHistoryRecord);
            this._feeDbRepository.Context.SaveChanges();

            // Act
            this._feeDbRepository.SeedDatabase(directoryInfo);

            // Assert
            this._feeDbRepository.Context.FeesDataSeedingHistories.Should().NotBeEmpty();
            this._feeDbRepository.Context.FeesDataSeedingHistories.Should()
                .OnlyContain(dt => dt.Equals(feesDataSeedingHistoryRecord));
            this._feeDbRepository.Context.ServiceRequestTypes.Should().HaveCount(serviceRequestCount);
        }

        [TestMethod]
        public void SeedDatabaseWhenNewerVersionFilesExistThenDatabaseChangesHappen()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                , "Resources");

            var directoryInfo = new DirectoryInfo(path);

            var serviceRequestCount = this._feeDbRepository.Context.ServiceRequestTypes!.Count();

            var feesDataSeedingHistoryRecord = new FeesDataSeedingHistory()
            {
                CreatedOn = DateTime.Now,
                Version = 1
            };

            this._feeDbRepository.Context.FeesDataSeedingHistories!.Add(feesDataSeedingHistoryRecord);
            this._feeDbRepository.Context.SaveChanges();

            var expectedSeedingHistoryVersion = 2;
            var expectedServiceRequestTypes = serviceRequestCount + 2;

            // Act
            this._feeDbRepository.SeedDatabase(directoryInfo);

            // Assert
            this._feeDbRepository.Context.FeesDataSeedingHistories.Should().NotBeEmpty();
            this._feeDbRepository.Context.FeesDataSeedingHistories.Should()
                .Contain(dt => dt.Equals(feesDataSeedingHistoryRecord));
            this._feeDbRepository.Context.ServiceRequestTypes.Should().HaveCount(expectedServiceRequestTypes);

            var latestFeesSeedHistoryRecord = this._feeDbRepository.Context
                            .FeesDataSeedingHistories
                            .OrderByDescending(o => o.Version)
                            .FirstOrDefault();

            latestFeesSeedHistoryRecord.Should().NotBeNull();
            latestFeesSeedHistoryRecord!.Id.Should().NotBe(feesDataSeedingHistoryRecord.Id);
            latestFeesSeedHistoryRecord.Version.Should().Be(expectedSeedingHistoryVersion);
        }
        #endregion
    }
}
