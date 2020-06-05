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
            EnvelopeSummary result;
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);

                var accountId = this.Configuration["Docusign:accountId"];
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var envelopeApi = new EnvelopesApi(ApiClient.Configuration);

                var envelopeDefinition = this.CreateEnvelopeFromTemplate(docusignTemplate);

                // This will send the document to the signer through email. We will use the webhook to detect when it was signed.
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

        #region Signing by Embedding
        [HttpPost]
        public IActionResult EmbeddedSigning(EnvelopeTemplate docusignTemplate)
        {
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);

                var accountId = this.Configuration["Docusign:accountId"];
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var envelopeApi = new EnvelopesApi(ApiClient.Configuration);

                const string signerId = "1000";
                var envelopeDefinition = this.CreateEnvelopeFromTemplate(docusignTemplate, signerId);
                var envelope = envelopeApi.CreateEnvelope(accountId, envelopeDefinition);

                var viewRequest = this.MakeRecipientViewRequest(envelopeDefinition.TemplateRoles[0].Email, envelopeDefinition.TemplateRoles[0].Name, signerId);

                // This will send the document to the signer through browser embedding. We will use the webhook to detect when it was signed.
                var result = envelopeApi.CreateRecipientView(accountId, envelope.EnvelopeId, viewRequest);

                return this.Redirect(result.Url);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }
        #endregion

        #region Helpers
        private EnvelopeDefinition CreateEnvelopeFromTemplate(EnvelopeTemplate docusignTemplate, string signerClientId = null)
        {
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

            // If we are using embedded signing, then the signer id in the envelope must match the signer id in the ReceipientView
            if (signerClientId != null)
            {
                envelopeDefinition.TemplateRoles[0].ClientUserId = signerClientId;
            }

            return envelopeDefinition;
        }

        /// <summary>
        /// This code comes from
        /// https://developers.docusign.com/esign-rest-api/code-examples/code-example-embedded-signing
        /// https://github.com/docusign/code-examples-csharp/blob/master/launcher-csharp/Controllers/Eg001EmbeddedSigningController.cs
        /// </summary>
        private RecipientViewRequest MakeRecipientViewRequest(string signerEmail, string signerName, string signerClientId = "1000")
        {
            // Data for this method
            // signerEmail 
            // signerName
            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global

            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            RecipientViewRequest viewRequest = new RecipientViewRequest
            {
                ReturnUrl =  $"{Request.Scheme}://{Request.Host}/Docusign?email={signerEmail}",

                // How has your app authenticated the user? In addition to your app's
                // authentication, you can include authenticate steps from DocuSign.
                // Eg, SMS authentication
                AuthenticationMethod = "none",

                // Recipient information must match embedded recipient info
                // we used to create the envelope.
                Email = signerEmail,
                UserName = signerName,
                ClientUserId = signerClientId,

                // DocuSign recommends that you redirect to DocuSign for the
                // Signing Ceremony. There are multiple ways to save state.
                // To maintain your application's session, use the pingUrl
                // parameter. It causes the DocuSign Signing Ceremony web page
                // (not the DocuSign server) to send pings via AJAX to your
                // app,
                PingFrequency = "600",  // seconds

                // NOTE: The pings will only be sent if the pingUrl is an https address
                PingUrl = $"{Request.Scheme}://{Request.Host}"  // optional setting
            };

            return viewRequest;
        }
        #endregion
   }
}