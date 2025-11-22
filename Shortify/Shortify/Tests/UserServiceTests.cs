using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shortify.DTOs.UserDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services;
using Shortify.Services.Interfaces;
using Xunit;

namespace Shortify.Tests
{
    public class UserServiceTests
    {
        private static IUserService CreateService(Mock<IUserRepository> repoMock, Mock<IAuthContext> authMock)
        {
            return new UserService(repoMock.Object, authMock.Object);
        }

        [Fact]
        public async Task CreateAsync_Succeeds_WhenEmailUnique()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(MockBehavior.Strict);
            var authMock = new Mock<IAuthContext>(MockBehavior.Strict);

            var tenantId = "tenant_test";
            var createDto = new CreateUserDto
            {
                TenantId = tenantId,
                Email = "new.user@shortify.test",
                DisplayName = "New User",
                Roles = new[] { "user" }
            };

            // Repo: GetByEmailAsync returns null (unique)
            repoMock
                .Setup(r => r.GetByEmailAsync(tenantId, createDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntity?)null)
                .Verifiable();

            // Repo: AddAsync should be called and return the created entity with Id
            repoMock
                .Setup(r => r.AddAsync(It.Is<UserEntity>(u =>
                    u.TenantId == tenantId &&
                    u.Email == createDto.Email &&
                    u.DisplayName == createDto.DisplayName
                ), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntity u, CancellationToken ct) =>
                {
                    u.Id = 42;
                    // ensure RolesJson serialized
                    if (string.IsNullOrWhiteSpace(u.RolesJson)) u.RolesJson = JsonSerializer.Serialize(createDto.Roles);
                    return u;
                })
                .Verifiable();

            // Auth: non-admin and tenant matches caller (should allow create)
            authMock.Setup(a => a.Role).Returns("user");
            authMock.Setup(a => a.TenantId).Returns(tenantId);

            var svc = CreateService(repoMock, authMock);

            // Act
            var result = await svc.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(42, result.Id);
            Assert.Equal(tenantId, result.TenantId);
            Assert.Equal(createDto.Email, result.Email);
            Assert.Equal(createDto.DisplayName, result.DisplayName);
            Assert.Contains("user", result.Roles);

            repoMock.VerifyAll();
        }

        [Fact]
        public async Task CreateAsync_ThrowsConflict_WhenEmailExists()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(MockBehavior.Strict);
            var authMock = new Mock<IAuthContext>(MockBehavior.Strict);

            var tenantId = "tenant_test";
            var createDto = new CreateUserDto
            {
                TenantId = tenantId,
                Email = "exists@shortify.test",
                DisplayName = "Exists User",
                Roles = Array.Empty<string>()
            };

            // Repo: GetByEmailAsync returns existing user -> conflict
            repoMock
                .Setup(r => r.GetByEmailAsync(tenantId, createDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntity
                {
                    Id = 1,
                    TenantId = tenantId,
                    Email = createDto.Email,
                    DisplayName = "Existing",
                    RolesJson = JsonSerializer.Serialize(Array.Empty<string>()),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                })
                .Verifiable();

            // Auth: admin or user doesn't matter here, repo returns existing and service should throw
            authMock.Setup(a => a.Role).Returns("admin");
            authMock.Setup(a => a.TenantId).Returns(tenantId);

            var svc = CreateService(repoMock, authMock);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(createDto));

            repoMock.VerifyAll();
        }

        [Fact]
        public async Task GetAll_ReturnsPaged_OnlyTenant()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(MockBehavior.Strict);
            var authMock = new Mock<IAuthContext>(MockBehavior.Strict);

            var callerTenant = "tenant_caller";
            // Simulate non-admin caller; service should use auth.TenantId when tenantId arg is null
            authMock.Setup(a => a.Role).Returns("user");
            authMock.Setup(a => a.TenantId).Returns(callerTenant);

            // Prepare repository to return two users for the callerTenant
            var seeded = new List<UserEntity>
            {
                new UserEntity { Id = 10, TenantId = callerTenant, Email = "u1@t.test", DisplayName = "U1", RolesJson = JsonSerializer.Serialize(new[] { "user" }), Status = "active", CreatedAt = DateTime.UtcNow },
                new UserEntity { Id = 11, TenantId = callerTenant, Email = "u2@t.test", DisplayName = "U2", RolesJson = JsonSerializer.Serialize(new[] { "user" }), Status = "active", CreatedAt = DateTime.UtcNow }
            };

            repoMock
                .Setup(r => r.GetAllAsync(callerTenant, 1, 50, It.IsAny<CancellationToken>()))
                .ReturnsAsync(seeded)
                .Verifiable();

            var svc = CreateService(repoMock, authMock);

            // Act: pass null as tenantId to ensure service picks auth.TenantId
            var result = await svc.GetAllAsync(null, page: 1, pageSize: 50);

            // Assert
            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(2, list.Count);
            Assert.Contains(list, u => u.Email == "u1@t.test");
            Assert.Contains(list, u => u.Email == "u2@t.test");

            repoMock.VerifyAll();
        }
    }
}
