using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Shortify.Services;
using Shortify.Repositories.Interfaces;
using Shortify.Models;

public class ResolveServiceForwardingTests
{
    [Fact]
    public async Task ResolveAsync_ForwardsSelectedHeadersAndQuerystring()
    {
        var attr = new AttributeEntity
        {
            Id = 200,
            AccountRootUserPk = 99,
            ResolveUrl = "https://upstream.test/resource",
            Visibility = "public",
            Status = "active"
        };

        var attrRepoMock = new Mock<IAttributeRepository>();
        attrRepoMock.Setup(r => r.GetByIdAsync(attr.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attr);

        var eventsRepoMock = new Mock<IResolveEventRepository>();
        var cacheMock = new Mock<IDistributedCache>();

        // Prepare HttpMessageHandler mock to inspect outgoing request URL and headers
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().StartsWith("https://upstream.test/resource") &&
                    req.RequestUri.Query.Contains("xtrace=1") &&
                    req.Headers.UserAgent.ToString().Contains("UnitTest") &&
                    req.Headers.Accept.ToString().Contains("application/json")
              ),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
           {
               Content = new StringContent("{\"ok\":true}", System.Text.Encoding.UTF8, "application/json")
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("resolve-fetcher")).Returns(httpClient);

        var loggerMock = new Mock<ILogger<ResolveService>>();
        var keyStoreMock = new Mock<Shortify.Core.Contracts.IKeyStore>();

        var service = new ResolveService(
            attrRepoMock.Object,
            eventsRepoMock.Object,
            factoryMock.Object,
            cacheMock.Object,
            loggerMock.Object,
            keyStoreMock.Object
        );

        // Build a fake HttpRequest for forwarding headers and querystring
        var context = new DefaultHttpContext();
        context.Request.Headers["User-Agent"] = "UnitTest-Agent";
        context.Request.Headers["Accept"] = "application/json";
        context.Request.QueryString = new QueryString("?xtrace=1");

        var result = await service.ResolveAsync(attr.AccountRootUserPk, attr.Id, context.Request, "json", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("application/json", result.ContentType);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
