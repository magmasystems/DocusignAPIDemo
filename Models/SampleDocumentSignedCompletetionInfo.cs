using System;

namespace DocusignAPIDemo.Models
{
    public class SampleDocumentSignedCompletetionInfo
    {
        public string Email { get; set; }
        public string DocumentId { get; set; }
        public string Status { get; set; }
        public DateTime TimeSigned { get; set; }
    }
}