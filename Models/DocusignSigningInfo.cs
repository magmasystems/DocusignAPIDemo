using DocuSign.eSign.Model;

namespace DocusignAPIDemo.Models
{
    public class DocusignSigningInfo
    {
        public EnvelopeTemplate EnvelopeTemplate { get; set; }
        public SampleCustomer Customer { get; set; }
    }
}