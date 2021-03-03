using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class Metadata : KeyValueEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
