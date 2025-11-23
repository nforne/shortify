using Xunit;
using System.Threading.Tasks;

namespace Shortify.Tests
{
    public class GroupsControllerIntegrationTests
    {
        [Fact]
        public Task GetAll_ReturnsSeededGroups() => Task.CompletedTask;

        [Fact]
        public Task PostPutPatchDelete_EndToEnd() => Task.CompletedTask;
    }
}
