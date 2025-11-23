using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shortify.Data;
using Shortify.DTOs.AttrDTOs;
using Shortify.Models;
using Shortify.Repositories;
using Shortify.Services;
using Xunit;

namespace Shortify.Tests
{
    public class AttributeServiceTests
    {
        private static ShortifyDbContext CreateInMemoryDb(string name)
        {
            var opts = new DbContextOptionsBuilder<ShortifyDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ShortifyDbContext(opts);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto()
        {
            using var db = CreateInMemoryDb(nameof(CreateAsync_ShouldCreateAndReturnDto));
            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "admin" && a.TenantId == "tenant_1" && a.UserId == "u1");
            var svc = new AttributeService(repo, auth);

            var dto = new CreateAttributeDto
            {
                TenantId = "tenant_1",
                Key = "campaign_source",
                ValueType = "string",
                Visibility = "public"
            };

            var result = await svc.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Key.Should().Be("campaign_source");
            (await db.Attributes.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_DuplicateKey_ThrowsConflict()
        {
            using var db = CreateInMemoryDb(nameof(CreateAsync_DuplicateKey_ThrowsConflict));
            db.Attributes.Add(new AttributeEntity { TenantId = "tenant_1", Key = "dup", Status = "active", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "admin" && a.TenantId == "tenant_1" && a.UserId == "u1");
            var svc = new AttributeService(repo, auth);

            var dto = new CreateAttributeDto { TenantId = "tenant_1", Key = "dup" };

            await Assert.ThrowsAsync<Services.ConflictException>(() => svc.CreateAsync(dto));
        }

        [Fact]
        public async Task GetById_PrivateAttribute_ForOtherTenant_ThrowsForbidden()
        {
            using var db = CreateInMemoryDb(nameof(GetById_PrivateAttribute_ForOtherTenant_ThrowsForbidden));
            db.Attributes.Add(new AttributeEntity { Id = 10, TenantId = "tenant_a", Key = "k", Visibility = "private", Status = "active", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "user" && a.TenantId == "tenant_b" && a.UserId == "u2");
            var svc = new AttributeService(repo, auth);

            await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetByIdAsync(10));
        }

        [Fact]
        public async Task ListAsync_Default_ReturnsOnlyPublicActiveNonExpired()
        {
            using var db = CreateInMemoryDb(nameof(ListAsync_Default_ReturnsOnlyPublicActiveNonExpired));
            db.Attributes.AddRange(
                new AttributeEntity { TenantId = "tenant_1", Key = "pub_active", Visibility = "public", Status = "active", CreatedAt = DateTime.UtcNow },
                new AttributeEntity { TenantId = "tenant_1", Key = "priv_active", Visibility = "private", Status = "active", CreatedAt = DateTime.UtcNow },
                new AttributeEntity { TenantId = "tenant_1", Key = "pub_expired", Visibility = "public", Status = "active", ExpiryDate = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "user" && a.TenantId == "tenant_1" && a.UserId == "u1");
            var svc = new AttributeService(repo, auth);

            var list = await svc.ListAsync("tenant_1", includePrivate: false);
            list.Select(x => x.Key).Should().ContainSingle("pub_active");
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesAttribute()
        {
            using var db = CreateInMemoryDb(nameof(DeleteAsync_SoftDeletesAttribute));
            var entity = new AttributeEntity { TenantId = "tenant_1", Key = "to_delete", Status = "active", CreatedAt = DateTime.UtcNow };
            db.Attributes.Add(entity);
            await db.SaveChangesAsync();

            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "admin" && a.TenantId == "tenant_1" && a.UserId == "u1");
            var svc = new AttributeService(repo, auth);

            await svc.DeleteAsync(entity.Id);

            var reloaded = await db.Attributes.FindAsync(entity.Id);
            reloaded.Status.Should().Be("deleted");
            reloaded.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ToggleVisibility_ChangesVisibility()
        {
            using var db = CreateInMemoryDb(nameof(ToggleVisibility_ChangesVisibility));
            var entity = new AttributeEntity { TenantId = "tenant_1", Key = "vis", Visibility = "public", Status = "active", CreatedAt = DateTime.UtcNow };
            db.Attributes.Add(entity);
            await db.SaveChangesAsync();

            var repo = new AttributeRepository(db);
            var auth = Mock.Of<IAuthContext>(a => a.Role == "admin" && a.TenantId == "tenant_1" && a.UserId == "u1");
            var svc = new AttributeService(repo, auth);

            await svc.ToggleVisibilityAsync(entity.Id, "private");
            var reloaded = await db.Attributes.FindAsync(entity.Id);
            reloaded.Visibility.Should().Be("private");
        }
    }
}
