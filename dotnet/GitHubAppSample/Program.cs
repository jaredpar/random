using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace app
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var appId = 69708;
            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.FilePrivateKeySource(@"C:\Users\jaredpar\Downloads\runfotest.2020-06-21.private-key.pem"),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = appId, // The GitHub App Id
                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();

            // Pass the JWT as a Bearer token to Octokit.net
            var appClient = new GitHubClient(new ProductHeaderValue("RunFoTest"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };

            var app = await appClient.GitHubApps.GetCurrent();

            // Get a list of installations for the authenticated GitHubApp
            var installations = await appClient.GitHubApps.GetAllInstallationsForCurrent();

            var installation = installations.Single();

            var token2 = await appClient.GitHubApps.CreateInstallationToken(installation.Id);
            var installationClient = new GitHubClient(new ProductHeaderValue($"RunfoTest-{installation.Id}"))
            {
                Credentials = new Credentials(token2.Token)
            };

            await installationClient.Issue.Comment.Create("jaredpar", "devops-util", 5, "See if this works");




        }
    }
}
