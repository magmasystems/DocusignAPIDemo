using System;
using System.Collections.Generic;
using System.Linq;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocusignAPIDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocusignAPIDemo.Services
{
    #region Interface

    #endregion

    public class DocusignService : IDocusignService
    {
        #region Variables
        private IDocusignAuthenticator DocusignAuthenticator { get; }
        private ILogger<DocusignService> Logger { get; }
        private IConfiguration Configuration { get; }
        
        public  ApiClient ApiClient { get; set; }
        private string AccountId { get; }
        #endregion

        #region Constructors
        public DocusignService(ILogger<DocusignService> logger, IConfiguration configuration, IDocusignAuthenticator authenticator)
        {
            this.Logger = logger;
            this.Configuration = configuration;
            this.DocusignAuthenticator = authenticator;

            this.AccountId = this.Configuration["Docusign:accountId"];
        }
        #endregion

        #region Cleanup
        public void Dispose()
        {
        }
        #endregion

        #region Queries
        public IEnumerable<EnvelopeTemplate> GetTemplates(Action<EnvelopeTemplate> callback = null)
        {               
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);
            
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var templates = templatesApi.ListTemplates(this.AccountId);

                if (callback != null)
                {
                    foreach (var template in templates.EnvelopeTemplates)
                    {
                        callback(template);
                    }
                }

                return templates.EnvelopeTemplates.ToList();
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }

        public EnvelopeTemplate GetTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return null;
            return this.FindTemplate(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
        }

        public EnvelopeTemplate FindTemplate(Predicate<EnvelopeTemplate> match)
        {            
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);
            
                var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var templates = templatesApi.ListTemplates(this.AccountId);

                foreach (var template in templates.EnvelopeTemplates)
                {
                    if (match(template))
                        return templatesApi.Get(this.AccountId, template.TemplateId);
                }
                return null;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }
        #endregion

        #region Signing
        public EnvelopeSummary SignThroughEmail(DocusignSigningInfo signingInfo)
        {
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);

                // var templatesApi = new TemplatesApi(ApiClient.Configuration);
                var envelopeApi = new EnvelopesApi(ApiClient.Configuration);

                var envelopeDefinition = this.CreateEnvelopeFromTemplate(signingInfo.EnvelopeTemplate, signingInfo.Customer);

                // This will send the document to the signer through email. We will use the webhook to detect when it was signed.
                var result = envelopeApi.CreateEnvelope(this.AccountId, envelopeDefinition);
                return result;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }

        public ViewUrl SignThroughEmbedding(HttpRequest httpRequest, DocusignSigningInfo signingInfo)
        {
            try
            {
                this.DocusignAuthenticator.Authenticate(ApiClient);

                var envelopeApi = new EnvelopesApi(ApiClient.Configuration);
                var envelopeDefinition = this.CreateEnvelopeFromTemplate(signingInfo.EnvelopeTemplate, signingInfo.Customer);
                var envelope = envelopeApi.CreateEnvelope(this.AccountId, envelopeDefinition);

                var viewRequest = this.MakeRecipientViewRequest(httpRequest, signingInfo.Customer, envelope.EnvelopeId);

                // This will send the document to the signer through browser embedding. We will use the webhook to detect when it was signed.
                var result = envelopeApi.CreateRecipientView(this.AccountId, envelope.EnvelopeId, viewRequest);

                return result;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e.Message);
                throw;
            }
        }
        #endregion

        #region Helpers
        private EnvelopeDefinition CreateEnvelopeFromTemplate(EnvelopeTemplate docusignTemplate, SampleCustomer customer)
        {
            var envelopeDefinition = new EnvelopeDefinition
            {
                TemplateId = docusignTemplate.TemplateId,
                Status = "sent",
                TemplateRoles = new List<TemplateRole> 
                {
                    new TemplateRole
                    {
                        Email = customer.Email, 
                        Name = customer.Name, 
                        RoleName = "Signer",
                        Tabs = new Tabs(TitleTabs: new List<Title> { new Title { Value = customer.Title } })
                    } 
                },
            };

            // If we are using embedded signing, then the signer id in the envelope must match the signer id in the ReceipientView
            if (!string.IsNullOrEmpty(customer.SignerId))
            {
                envelopeDefinition.TemplateRoles[0].ClientUserId = customer.SignerId;
            }

            return envelopeDefinition;
        }

        /// <summary>
        /// This code comes from
        /// https://developers.docusign.com/esign-rest-api/code-examples/code-example-embedded-signing
        /// https://github.com/docusign/code-examples-csharp/blob/master/launcher-csharp/Controllers/Eg001EmbeddedSigningController.cs
        /// </summary>
        private RecipientViewRequest MakeRecipientViewRequest(HttpRequest request, SampleCustomer customer, string envelopeId)
        {
            // Construct the return url that Docusign will use when the signing ceremony is over
            var returnUrlFromConfig = this.Configuration["DocuSign:returnUrlForEmbeddedSigning"];
            var relativePath = returnUrlFromConfig ?? "Docusign/EmbeddedSigningProcessor";
            if (relativePath.StartsWith('/'))  // strip leading slash
                relativePath = relativePath.Substring(1);

            string returnUrl = 
                 $"{request.Scheme}://{request.Host}/{relativePath}?email={customer.Email}&envelopeId={envelopeId}";

            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            RecipientViewRequest viewRequest = new RecipientViewRequest
            {
                ReturnUrl = returnUrl,

                // How has your app authenticated the user? In addition to your app's
                // authentication, you can include authenticate steps from DocuSign.
                // Eg, SMS authentication
                AuthenticationMethod = "none",

                // Recipient information must match embedded recipient info
                // we used to create the envelope.
                Email = customer.Email,
                UserName = customer.Name,
                ClientUserId = customer.SignerId,

                // DocuSign recommends that you redirect to DocuSign for the
                // Signing Ceremony. There are multiple ways to save state.
                // To maintain your application's session, use the pingUrl
                // parameter. It causes the DocuSign Signing Ceremony web page
                // (not the DocuSign server) to send pings via AJAX to your
                // app,
                PingFrequency = "600",  // seconds

                // NOTE: The pings will only be sent if the pingUrl is an https address
                PingUrl = $"{request.Scheme}://{request.Host}"  // optional setting
            };

            return viewRequest;
        }
        #endregion
    }
}