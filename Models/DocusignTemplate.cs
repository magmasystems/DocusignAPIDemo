using DocuSign.eSign.Model;

namespace DocusignAPIDemo.Models
{
    public class DocusignTemplate
    {
        private EnvelopeTemplate Template { get; }
        
        public DocusignTemplate(EnvelopeTemplate template)
        {
            this.Template = template;
        }
    }
}