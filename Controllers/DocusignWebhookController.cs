using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using DocusignAPIDemo.Models;

namespace DocusignAPIDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocusignWebhookController : ControllerBase
    {
        [HttpPost]
        public ActionResult<RobynDocumentSignedCompletetionInfo> Index()  // IMPORTANT: DO NOT USE AN ARGUMENT TO THIS FUNCTION
        {
            if (!(new XmlSerializer(typeof(DocuSignEnvelopeInformation)).Deserialize(Request.Body) is DocuSignEnvelopeInformation envelopeInformation))
                return null;
            
            var documentSignedCompletetionInfo = new RobynDocumentSignedCompletetionInfo
            {
                Email = envelopeInformation.EnvelopeStatus.RecipientStatuses[0].RecipientStatus.Email,
                DocumentId = envelopeInformation.EnvelopeStatus.EnvelopeID,
                Status = envelopeInformation.EnvelopeStatus.Status,
                TimeSigned = envelopeInformation.EnvelopeStatus.Completed,
            };
            
            return documentSignedCompletetionInfo;
        }
    }
}