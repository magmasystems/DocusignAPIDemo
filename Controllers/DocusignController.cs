using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocuSign.eSign.Client;
using DocusignAPIDemo.Services;
using System;
using DocusignAPIDemo.Models;

namespace DocusignAPIDemo.Controllers
{
    public class DocusignController : Controller
    {
        #region Variables
        private ILogger<DocusignController> Logger { get; }
        private IConfiguration Configuration { get; }
        private IDocusignService DocusignService{ get; }
        private SampleCustomer Customer { get; set; }
        private string TemplateName { get; set; }
        #endregion

        #region Constructors
        public DocusignController(ILogger<DocusignController> logger, IConfiguration config, IDocusignService docusignService)
        {
            this.Logger = logger;
            this.Configuration = config;

            this.DocusignService = docusignService;
            this.DocusignService.ApiClient = new ApiClient(this.Configuration["Docusign:url"]);

            // Sample data
            this.Customer = new SampleCustomer 
            { 
                Name = config["SampleData:Customer:Name"], 
                Email = config["SampleData:Customer:Email"], 
                Title = config["SampleData:Customer:Title"], 
                SignerId = config["SampleData:Customer:SignerId"] 
            };
            this.TemplateName = config["SampleData:TemplateName"];
        }
        #endregion

        #region Gets a template from DocuSign
        [HttpGet]
        public IActionResult Index(string @event, string email)
        {      
            // After an embedded signing, the event should be "signing_complete" and the email should be the email of the signer
            if (!string.IsNullOrEmpty(@event))
            {
                Console.WriteLine(@event);

                if (!string.IsNullOrEmpty(email))
                    Console.WriteLine(email);
            }

            // Get the sample template
            var docusignTemplate = this.DocusignService.GetTemplate(this.TemplateName);
            return View("Index", new DocusignSigningInfo { EnvelopeTemplate = docusignTemplate, Customer = this.Customer });
        }
        #endregion

        #region Sends a template to be signed by a recipient
        [HttpPost]
        public IActionResult Send(DocusignSigningInfo signingInfo)
        {
            var result = this.DocusignService.SignThroughEmail(signingInfo);
            return View("DocumentSendResult", result);
        }
        #endregion

        #region Signing by Embedding
        [HttpPost]
        public IActionResult EmbeddedSigning(DocusignSigningInfo signingInfo)
        {
            var result = this.DocusignService.SignThroughEmbedding(this.Request, signingInfo);
            return this.Redirect(result.Url);
        }

        [HttpGet]
        public IActionResult EmbeddedSigningProcessor(string email, string @event, string envelopeId)
        {
            // After an embedded signing, the event should be "signing_complete" and the email should be the email of the signer
            if (!string.IsNullOrEmpty(@event))
            {
                Console.WriteLine($"Event: {@event}, Email: {email}, EnvId: {envelopeId}");
            }

            // At this point, assuming that there is a customer record in the database that has a field that records
            // the status and the time of the signing, we can update the database before returning to the main web page.

            return LocalRedirect("~/Docusign");
        }
        #endregion
   }
}