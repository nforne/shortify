using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shortify.Controllers;
using Shortify.DTOs.AttrDTOs;
using Shortify.Services.Interfaces;
using Xunit;

namespace Shortify.Tests
{
    public class AttributesControllerTests
    {
        [Fact]
        public async Task Post_ReturnsCreated()
        {
            var svc = new Mock<IAttributeService>();
            svc.Setup(s => s.CreateAsync(It.IsAny<CreateAttributeDto>(), default)).ReturnsAsync(new AttributeDto { Id = 1, Key = "k", TenantId = "t" });

            var ctrl = new AttributesController(svc.Object);
            var res = await ctrl.Post(new CreateAttributeDto { TenantId = "t", Key = "k" }) as CreatedAtActionResult;

            res.Should().NotBeNull();
            res.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var svc = new Mock<IAttributeService>();
            svc.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(new AttributeDto { Id = 1, Key = "k", TenantId = "t" });

            var ctrl = new AttributesController(svc.Object);
            var res = await ctrl.GetById(1) as OkObjectResult;

            res.Should().NotBeNull();
            ((AttributeDto)res.Value).Id.Should().Be(1);
        }
    }
}
