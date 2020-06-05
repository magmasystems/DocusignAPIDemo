using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocusignAPIDemo.Services;

namespace DocusignAPIDemo.Controllers
{
    public class DocusignController : Controller
    {
        #region Variables
        private ILogger<DocusignController> Logger { get; }
        private IConfiguration Configuration { get; }
        private IDocusignAuthenticator DocusignAuthenticator { get; }
        private static ApiClient ApiClient { get; set; }
        #endregion

        #region Constructors
        public DocusignController(ILogger<DocusignController> logger, IConfiguration config, IDocusignAuthenticator authenticator)
        {
            this.Logger = logger;
            this.Configuration = config;
            this.DocusignAuthenticator = authenticator;
            
            var basePath = this.Configuration["Docusign:url"];
            ApiClient = new ApiClient(basePath);
        }
        #endregion
        
        #region Gets a template from DocuSign
        [HttpGet]
        public IActionResult Index()
        {
            EnvelopeTemplate docusignTemplate = null;
            
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);
            
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
                this.DocusignAuthenticator.Authenticate(ApiClient);

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
   }
}