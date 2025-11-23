using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shortify.Controllers;
using Shortify.DTOs.MetricDTOs;
using Shortify.Services.Interfaces;
using Xunit;

namespace Shortify.Tests
{
    public class MetricsControllerTests
    {
        //[Fact]
        //public async Task Post_ReturnsCreated()
        //{
        //    var svc = new Mock<IMetricService>();
        //    svc.Setup(s => s.GenerateAsync(It.IsAny<CreateMetricDto>(), default))
        //        .ReturnsAsync(new MetricDto { Id = 1, Name = "m", TenantId = "t", Status = "ready" });

        //    var ctrl = new MetricsController(svc.Object);
        //    var res = await ctrl.Post(new CreateMetricDto { TenantId = "t", Name = "m", From = DateTime.UtcNow.AddDays(-1), To = DateTime.UtcNow }) as CreatedAtActionResult;

        //    res.Should().NotBeNull();
        //    res.StatusCode.Should().Be(201);
        //}

        [Fact]
        public async Task PostJson_ReturnsCreated()
        {
            var svc = new Mock<IMetricService>();
            svc.Setup(s => s.GenerateAsync(It.IsAny<CreateJsonMetricDto>(), default))
                .ReturnsAsync(new MetricDto { Id = 2, Name = "m2", TenantId = "t", Status = "ready" });

            var ctrl = new MetricsController(svc.Object);
            var res = await ctrl.PostJson(new CreateJsonMetricDto { TenantId = "t", Name = "m2", From = DateTime.UtcNow.AddDays(-1), To = DateTime.UtcNow }) as CreatedAtActionResult;

            res.Should().NotBeNull();
            res.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var svc = new Mock<IMetricService>();
            svc.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(new MetricDto { Id = 1, Name = "m", TenantId = "t" });

            var ctrl = new MetricsController(svc.Object);
            var res = await ctrl.GetById(1) as OkObjectResult;

            res.Should().NotBeNull();
            ((MetricDto)res.Value).Id.Should().Be(1);
        }

        [Fact]
        public async Task Download_ReturnsFile_WhenExists()
        {
            var svc = new Mock<IMetricService>();
            svc.Setup(s => s.DownloadAsync(1, default)).ReturnsAsync(new byte[] { 1, 2, 3 });

            var ctrl = new MetricsController(svc.Object);
            var res = await ctrl.Download(1) as FileContentResult;

            res.Should().NotBeNull();
            res.FileDownloadName.Should().Contain("metric_1");
        }
    }
}
