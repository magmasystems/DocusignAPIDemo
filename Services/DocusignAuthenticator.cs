using System;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using Microsoft.Extensions.Configuration;

namespace DocusignAPIDemo.Services
{
    public interface IDocusignAuthenticator
    {
        bool UseOldStyleAuthentication { get; set; }

        void Authenticate(ApiClient apiClient);
    }

    public class DocusignAuthenticator : IDocusignAuthenticator
    {
        private IConfiguration Configuration { get; }
        private static string AccessToken { get; set; }
        private static DateTime ExpiresIn;

        public bool UseOldStyleAuthentication { get; set; } = false;  // this shuld come from the config file

        public DocusignAuthenticator(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void Authenticate(ApiClient apiClient)
        {
            if (this.UseOldStyleAuthentication)
                this.ConfigureApiClientAuth_OldMethod(apiClient);
            else
                this.ConfigureApiClientAuth(apiClient);
        }

        private void ConfigureApiClientAuth_OldMethod(ApiClient apiClient)
        {
            var username = this.Configuration["Docusign:username"];
            var password = this.Configuration["Docusign:password"];
            var integratorKey = this.Configuration["Docusign:clientId"];
            var authHeader = $"{{\"Username\":\"{username}\", \"Password\":\"{password}\", \"IntegratorKey\":\"{integratorKey}\"}}";
            apiClient.Configuration.AddDefaultHeader("X-DocuSign-Authentication", authHeader);

            var authApi = new AuthenticationApi();
            authApi.Login();
        }

        private void ConfigureApiClientAuth(ApiClient apiClient)
        {
            this.CheckToken(apiClient);
            apiClient.Configuration.AddDefaultHeader("Authorization", $"Bearer {AccessToken}");
        }
        
        private void CheckToken(ApiClient apiClient)
        {
            if (AccessToken != null && DateTime.UtcNow <= ExpiresIn) 
                return;
            
            this.UpdateToken(apiClient);
        }
        
        private void UpdateToken(ApiClient apiClient)
        {
            var clientId = this.Configuration["Docusign:clientId"];
            var impersonatedUserGuid = this.Configuration["Docusign:userId"];
            var authServer = this.Configuration["Docusign:authServer"];
            var privateKey = System.IO.File.ReadAllBytes(this.Configuration["Docusign:privateKeyFile"]);
            
            var authToken = apiClient.RequestJWTUserToken(
                clientId,
                impersonatedUserGuid,
                authServer,
                privateKey,
                1);

            AccessToken = authToken.access_token;
            if (authToken.expires_in != null) 
                ExpiresIn = DateTime.UtcNow.AddSeconds(authToken.expires_in.Value);
        }
    }
}