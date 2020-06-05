using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
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
        
        private static ApiClient ApiClient { get; set; }
        private RobynCustomer Customer { get; set; }
        #endregion

        #region Constructors
        public DocusignController(ILogger<DocusignController> logger, IConfiguration config, IDocusignService docusignService)
        {
            this.Logger = logger;
            this.Configuration = config;
            this.DocusignService = docusignService;
            
            var basePath = this.Configuration["Docusign:url"];
            ApiClient = new ApiClient(basePath);

            this.Customer = new RobynCustomer { Name = "John Reynolds", Email = "magmasystems@yahoo.com", Title = "Head Honcho", SignerId = "666" };
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

            // Get the first and only template
            var docusignTemplate = this.DocusignService.FindTemplate(ApiClient, t => true);
            return View("Index", new DocusignSigningInfo { EnvelopeTemplate = docusignTemplate, Customer = this.Customer });
        }
        #endregion

        #region Sends a template to be signed by a recipient
        [HttpPost]
        public IActionResult Send(DocusignSigningInfo signingInfo)
        {
            var result = this.DocusignService.SignThroughEmail(ApiClient, signingInfo);
            return View("DocumentSendResult", result);
        }
        #endregion

        #region Signing by Embedding
        [HttpPost]
        public IActionResult EmbeddedSigning(DocusignSigningInfo signingInfo)
        {
            var result = this.DocusignService.SignThroughEmbedding(this.Request, ApiClient, signingInfo);
            return this.Redirect(result.Url);
        }
        #endregion
   }
}