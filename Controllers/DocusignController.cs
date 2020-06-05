using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocusignAPIDemo.Models;

namespace DocusignAPIDemo.Controllers
{
    public class DocusignController : Controller
    {
        #region Variables
        private ILogger<DocusignController> Logger { get; }
        private IConfiguration Configuration { get; }
        private static ApiClient ApiClient { get; set; }
        #endregion

        #region Constructors
        public DocusignController(ILogger<DocusignController> logger, IConfiguration config)
        {
            this.Logger = logger;
            this.Configuration = config;
            
            var basePath = this.Configuration["Docusign:url"];
            ApiClient = new ApiClient(basePath);
            //this.ConfigureApiClientAuth(ApiClient);
        }
        #endregion
        
        #region Gets a template from DocuSign
        [HttpGet]
        public IActionResult Index()
        {
            EnvelopeTemplate docusignTemplate = null;
            
            try
            {
                this.ConfigureApiClientAuth(ApiClient);
            
                var accountId = this.Configuration["Docusign:accountId"];
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var templates = templatesApi.ListTemplates(accountId);

                foreach (var template in templates.EnvelopeTemplates)
                {
                    docusignTemplate = template;
                }

                docusignTemplate = templatesApi.Get(accountId, docusignTemplate.TemplateId);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
            
            return View("Index", docusignTemplate);
        }
        #endregion

        #region Sends a template to be signed by a recipient
        [HttpPost]
        public IActionResult Send(EnvelopeTemplate docusignTemplate)
        {
            EnvelopeSummary result = null;
            
            try
            {
                var accountId = this.Configuration["Docusign:accountId"];
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var envelopeApi = new EnvelopesApi(ApiClient.Configuration);

                var envelopeDefinition = new EnvelopeDefinition
                {
                    TemplateId = docusignTemplate.TemplateId,
                    Status = "sent",
                    TemplateRoles = new List<TemplateRole> 
                    {
                        // TODO: his is just some sample data. Replace it with the actual recipient.
                        new TemplateRole
                        {
                            Email = "magmasystems@yahoo.com", 
                            Name = "John Reynolds", 
                            RoleName = "Signer",
                            Tabs = new Tabs(TitleTabs: new List<Title> { new Title { Value = "Head Honcho" } })
                        } 
                    },
                };

                result = envelopeApi.CreateEnvelope(accountId, envelopeDefinition);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
            
            return View("DocumentSendResult", result);
        }
        #endregion

        #region DocuSign Authentication
        #if NOTUSED
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
        #endif

        private void ConfigureApiClientAuth(ApiClient apiClient)
        {
            this.CheckToken();
            apiClient.Configuration.AddDefaultHeader("Authorization", $"Bearer {AccessToken}");
        }
        
        private static string AccessToken { get; set; }
        private static DateTime ExpiresIn;
        
        public void CheckToken()
        {
            if (AccessToken != null && DateTime.UtcNow <= ExpiresIn) 
                return;
            
            this.UpdateToken();
        }
        
        private void UpdateToken()
        {
            var clientId = this.Configuration["Docusign:clientId"];
            var impersonatedUserGuid = this.Configuration["Docusign:userId"];
            var authServer = this.Configuration["Docusign:authServer"];
            var privateKey = System.IO.File.ReadAllBytes(this.Configuration["Docusign:privateKeyFile"]);
            
            var authToken = ApiClient.RequestJWTUserToken(
                clientId,
                impersonatedUserGuid,
                authServer,
                privateKey,
                1);

            AccessToken = authToken.access_token;
            if (authToken.expires_in != null) 
                ExpiresIn = DateTime.UtcNow.AddSeconds(authToken.expires_in.Value);
        }
        #endregion
    }
}