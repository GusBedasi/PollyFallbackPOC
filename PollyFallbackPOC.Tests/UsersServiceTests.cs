using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Moq.Protected;
using Polly;
using Polly.Fallback;
using Polly.Registry;
using PollyFallbackPOC.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PollyFallbackPOC.Tests
{
    public class UsersServiceTests
    {
        [Fact]
        public async Task GivenAGetUsersRequest_WhenTheRequestSucceeds_ThenReturnsAValidResponse()
        {
            var httpResponse = "Some response";
            var cancellationToken = new CancellationTokenSource().Token;

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(httpResponse)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://teste.com/v1/core/stub")
            };

            var policyRegistry = new PolicyRegistry
            {
                { "GetUsersFallback", Policy.NoOpAsync<string>() }
            };

            var sut = new UsersService(httpClient, policyRegistry);            

            var response = await sut.GetUsers(cancellationToken);

            response.Should().NotBeNull();
            response.Should().BeOfType<string>();
            response.Should().Be(httpResponse);
        }

        [Fact]
        public async Task GivenAGetUsersRequest_WhenExecuted_ThenShouldPassThroughThePolicy()
        {
            var mocker = new AutoMocker();

            var httpResponse = "Some response";
            var cancellationToken = new CancellationTokenSource().Token;

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(httpResponse)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://teste.com/v1/core/stub")
            };

            var policy = mocker.GetMock<IAsyncPolicy<string>>();

            var policyRegistry = new PolicyRegistry
            {
                { "GetUsersFallback", policy.Object }
            };

            mocker.Setup<IAsyncPolicy<string>, Task<string>>(
                    x => x.ExecuteAsync(It.IsAny<Func<Task<string>>>())
            ).ReturnsAsync("SomeResponse");

            mocker.Setup<IReadOnlyPolicyRegistry<string>, IAsyncPolicy<string>>(
                x => x.Get<IAsyncPolicy<string>>(It.IsAny<string>())
            ).Returns(policy.Object);

            var sut = new UsersService(httpClient, policyRegistry);

            var response = await sut.GetUsers(cancellationToken);

            mocker.Verify<IAsyncPolicy<string>, Task<string>>(x => x.ExecuteAsync(It.IsAny<Func<Task<string>>>()), Times.Once);
        }

        [Fact]
        public async Task GivenAGetUsersRequest_WhenTheFirstExternalAPIFails_ThenShouldMakeASecondRequest()
        {
            var mocker = new AutoMocker();

            var cancellationToken = new CancellationTokenSource().Token;

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://teste.com/v1/core/stub")
            };

            var policyRegistry = new PolicyRegistry
            {
                { "GetUsersFallback", CreateFallbackPolicy() }
            };

            mocker.Setup<IAsyncPolicy<string>, Task<string>>(
                    x => x.ExecuteAsync(It.IsAny<Func<Task<string>>>())
            ).Throws<HttpRequestException>();

            var sut = new UsersService(httpClient, policyRegistry);

            var response = await sut.GetUsers(cancellationToken);

            response.Should().NotBeNull();
            response.Should().BeOfType<string>();
            response.Should().Be("Some Response");
        }

        private AsyncFallbackPolicy<string> CreateFallbackPolicy()
        {
            return Policy<string>
                .Handle<HttpRequestException>(x => x.StatusCode >= HttpStatusCode.BadRequest && x.StatusCode <= HttpStatusCode.InternalServerError)
                .FallbackAsync(async (cancellationToken) =>
                {
                    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
                    mockHttpMessageHandler
                        .Protected()
                        .Setup<Task<HttpResponseMessage>>("SendAsync",
                            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                        .ReturnsAsync(new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent("Some Response")
                        });

                    var httpClient = new HttpClient(mockHttpMessageHandler.Object)
                    {
                        BaseAddress = new Uri("https://teste.com/v1/core/stub")
                    };

                    //Execute the request but this time with the right resource
                    var response = await httpClient.GetStringAsync("/users", cancellationToken);

                    return response;
                });
        }
    }
}
