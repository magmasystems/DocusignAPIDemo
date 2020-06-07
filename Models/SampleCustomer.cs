namespace DocusignAPIDemo.Models
{
    public class SampleCustomer
    {
        public string Name  { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string SignerId { get; set; }  // must be null if sending through email, non-null if embedding
    }
}