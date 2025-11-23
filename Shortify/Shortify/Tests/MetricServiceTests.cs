using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shortify.Data;
using Shortify.DTOs.MetricDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services;
using Shortify.Services.Interfaces;
using Xunit;

namespace Shortify.Tests
{
    public class MetricServiceTests
    {
        private readonly Mock<IMetricRepository> _repo = new();
        private readonly Mock<ISnapshotStorage> _storage = new();
        private readonly Mock<IAuthContext> _auth = new();

        private MetricService CreateService(string dbName = null)
        {
            // ensure unique in-memory DB per test when needed
            var options = new DbContextOptionsBuilder<ShortifyDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            var db = new ShortifyDbContext(options);

            // if you want the repository to use the real db instead of the mocked repo, construct it:
            // var repo = new MetricRepository(db);
            // otherwise re-use your mock _repo.Object (both are acceptable depending on test style)

            _auth.SetupGet(a => a.Role).Returns("admin");
            _auth.SetupGet(a => a.TenantId).Returns("tenant_abc123");
            _auth.SetupGet(a => a.UserId).Returns("test-user");

            // pass the db into the service constructor
            return new MetricService(_repo.Object, _storage.Object, _auth.Object, db);
        }


        [Fact]
        public async Task GenerateAsync_CreatesArchiveAndUploads_ReturnsReadyDto()
        {
            var svc = CreateService();

            // repo: no existing
            _repo.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MetricEntity?)null);
            _repo.Setup(r => r.AddAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MetricEntity e, CancellationToken _) => { e.Id = 1; return e; });
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // storage returns url
            _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("metrics/tenant_abc123/metric_campaign_source/20230101_20230131.zip", "file://tmp.zip", 1234L));

            var dto = new CreateMetricDto
            {
                TenantId = "tenant_abc123",
                Name = "metric_campaign_source",
                From = new DateTime(2023, 1, 1),
                To = new DateTime(2023, 1, 31),
                IncludeRaw = true
            };

            var result = await svc.GenerateAsync(dto, CancellationToken.None);

            result.Should().NotBeNull();
            result.Status.Should().Be("ready");
            result.StorageUrl.Should().Be("file://tmp.zip");
            result.FileSizeBytes.Should().Be(1234L);

            _repo.Verify(r => r.AddAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GenerateAsync_OverwriteExisting_UpdatesEntity()
        {
            var svc = CreateService();

            var existing = new MetricEntity
            {
                Id = 2,
                TenantId = "tenant_abc123",
                Name = "metric_campaign_source",
                From = new DateTime(2023, 1, 1),
                To = new DateTime(2023, 1, 31),
                Status = "ready",
                StorageKey = "metrics/tenant_abc123/metric_campaign_source/20230101_20230131.zip",
                StorageUrl = "file://old.zip",
                FileSizeBytes = 1000
            };

            _repo.Setup(r => r.GetByKeyAsync(existing.TenantId, existing.Name, existing.From, existing.To, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("metrics/tenant_abc123/metric_campaign_source/20230101_20230131.zip", "file://new.zip", 4321L));

            var dto = new CreateMetricDto
            {
                TenantId = "tenant_abc123",
                Name = "metric_campaign_source",
                From = existing.From,
                To = existing.To,
                IncludeRaw = false
            };

            var result = await svc.GenerateAsync(dto, CancellationToken.None);

            result.Should().NotBeNull();
            result.Status.Should().Be("ready");
            result.StorageUrl.Should().Be("file://new.zip");
            result.FileSizeBytes.Should().Be(4321L);
            _repo.Verify(r => r.UpdateAsync(It.Is<MetricEntity>(m => m.StorageUrl == "file://new.zip"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GenerateAsync_WhenUploadFails_SetsFailedStatus()
        {
            var svc = CreateService();

            _repo.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MetricEntity?)null);
            _repo.Setup(r => r.AddAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MetricEntity e, CancellationToken _) => { e.Id = 3; return e; });
            _repo.Setup(r => r.UpdateAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("S3 put failed"));

            var dto = new CreateMetricDto
            {
                TenantId = "tenant_abc123",
                Name = "metric_campaign_source",
                From = new DateTime(2023, 1, 1),
                To = new DateTime(2023, 1, 31)
            };

            await Assert.ThrowsAsync<Exception>(() => svc.GenerateAsync(dto, CancellationToken.None));

            _repo.Verify(r => r.UpdateAsync(It.Is<MetricEntity>(m => m.Status == "failed" && !string.IsNullOrEmpty(m.ErrorMessage)), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteAsync_RemovesStorageAndRecord()
        {
            var svc = CreateService();

            var e = new MetricEntity
            {
                Id = 5,
                TenantId = "tenant_abc123",
                Name = "m",
                From = DateTime.UtcNow.AddDays(-1),
                To = DateTime.UtcNow,
                Status = "ready",
                StorageKey = "metrics/tenant_abc123/m/20230101_20230102.zip"
            };

            _repo.Setup(r => r.GetByIdAsync(e.Id, It.IsAny<CancellationToken>())).ReturnsAsync(e);
            _repo.Setup(r => r.DeleteAsync(It.IsAny<MetricEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _storage.Setup(s => s.DeleteAsync(e.StorageKey, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await svc.DeleteAsync(e.Id);

            _storage.Verify(s => s.DeleteAsync(e.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.DeleteAsync(It.Is<MetricEntity>(me => me.Id == e.Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_ReturnsByteArray_WhenExists()
        {
            var svc = CreateService();
            var e = new MetricEntity { Id = 6, TenantId = "tenant_abc123", StorageKey = "k", Status = "ready" };

            _repo.Setup(r => r.GetByIdAsync(e.Id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

            var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            _storage.Setup(s => s.GetReadStreamAsync("k", It.IsAny<CancellationToken>())).ReturnsAsync(ms);

            var bytes = await svc.DownloadAsync(e.Id);
            bytes.Should().NotBeNull();
            bytes.Length.Should().Be(3);
        }
    }
}
