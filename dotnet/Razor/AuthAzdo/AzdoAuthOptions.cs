using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthAzdo
{
    public class AzdoAuthenticationOptions : OAuthOptions
    {
        public AzdoAuthenticationOptions()
        {
            ClaimsIssuer = AzdoAuthenticationDefaults.Issuer;
            CallbackPath = AzdoAuthenticationDefaults.CallbackPath;
            AuthorizationEndpoint = AzdoAuthenticationDefaults.AuthorizationEndPoint;
            TokenEndpoint = AzdoAuthenticationDefaults.TokenEndPoint;
            UserInformationEndpoint = AzdoAuthenticationDefaults.UserInformationEndPoint;

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "emailAddress");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        }
    }

    public static class AzdoAuthenticationDefaults
    {
        public const string AuthenticationScheme = "Azdo";
        public const string Display = "Azdo";
        public const string Issuer = "Azdo";
        public const string CallbackPath = "/signin-azdo";
        public const string AuthorizationEndPoint = "https://app.vssps.visualstudio.com/oauth2/authorize";
        public const string TokenEndPoint = "https://app.vssps.visualstudio.com/oauth2/token";
        public const string UserInformationEndPoint = "https://app.vssps.visualstudio.com/_apis/profile/profiles/me";
    }

    public static class AzdoExtensions
    {
        public static AuthenticationBuilder AddAzdo(this AuthenticationBuilder builder, Action<AzdoAuthenticationOptions> configuration) =>
            builder.AddOAuth<AzdoAuthenticationOptions, AzdoAuthenticationHandler>(
                AzdoAuthenticationDefaults.AuthenticationScheme,
                AzdoAuthenticationDefaults.Display,
                configuration);
    }

    public class AzdoAuthenticationHandler : OAuthHandler<AzdoAuthenticationOptions>
    {
        public AzdoAuthenticationHandler(
            IOptionsMonitor<AzdoAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {

        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            using var response = await Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                response.StatusCode,
                                response.Headers.ToString(),
                                await response.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var principal = new ClaimsPrincipal(identity);
            var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            context.RunClaimActions();

            /*
            // When the email address is not public, retrieve it from
            // the emails endpoint if the user:email scope is specified.
            if (!string.IsNullOrEmpty(Options.UserEmailsEndpoint) &&
                !identity.HasClaim(claim => claim.Type == ClaimTypes.Email) &&
                Options.Scope.Contains("user:email"))
            {
                string address = await GetEmailAsync(tokens);

                if (!string.IsNullOrEmpty(address))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, address, ClaimValueTypes.String, Options.ClaimsIssuer));
                }
            }
            */

            await Options.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["redirect_uri"] = context.RedirectUri,
                ["client_assertion"] = Options.ClientSecret,
                ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                ["assertion"] = context.Code,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            });

            using var response = await Backchannel.SendAsync(request, Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving an access token: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                response.StatusCode,
                                response.Headers.ToString(),
                                await response.Content.ReadAsStringAsync());

                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
            }

            var content = await response.Content.ReadAsStringAsync();
            var payload = JsonDocument.Parse(content);

            return OAuthTokenResponse.Success(payload);
        }
    }
}
