using Polly;
using Polly.Registry;

namespace PollyFallbackPOC.Services
{
    public class UsersService : IUsersService
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy<string> _policy;

        public UsersService(HttpClient httpClient, IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            _httpClient = httpClient;
            _policy = policyRegistry.Get<IAsyncPolicy<string>>("GetUsersFallback");
        }

        public async Task<string> GetUsers(CancellationToken cancellationToken)
        {
            //Executes the request which will return an error because of a typo on the resource name
            var response = await _policy.ExecuteAsync(async () =>
            {
                return await _httpClient.GetStringAsync("/userss", cancellationToken);
            });

            return response;
        }
    }
}
