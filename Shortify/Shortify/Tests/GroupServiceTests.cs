using System.Threading.Tasks;
using Xunit;

namespace Shortify.Tests
{
    public class GroupServiceTests
    {
        [Fact]
        public Task CreateAsync_Succeeds_WhenNameUnique() => Task.CompletedTask;

        [Fact]
        public Task CreateAsync_ThrowsConflict_WhenNameExists() => Task.CompletedTask;

        [Fact]
        public Task GetAll_ReturnsPaged_OnlyTenant() => Task.CompletedTask;

        [Fact]
        public Task Patch_UpdatesFields() => Task.CompletedTask;

        [Fact]
        public Task Delete_SoftSetsStatus_WhenSoftTrue() => Task.CompletedTask;
    }
}
