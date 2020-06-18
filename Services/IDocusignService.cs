using System;
using System.Collections.Generic;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocusignAPIDemo.Models;
using Microsoft.AspNetCore.Http;

namespace DocusignAPIDemo.Services
{
    public interface IDocusignService : IDisposable
    {
        ApiClient ApiClient { get; set; }

        IEnumerable<EnvelopeTemplate> GetTemplates(Action<EnvelopeTemplate> callback = null);
        EnvelopeTemplate GetTemplate(string templateName);
        EnvelopeTemplate FindTemplate(Predicate<EnvelopeTemplate> match);
        EnvelopeSummary SignThroughEmail(DocusignSigningInfo signingInfo);
        ViewUrl SignThroughEmbedding(HttpRequest httpRequest, DocusignSigningInfo signingInfo);
    }
}