using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.Entity
{
    public class Metadata : KeyValueEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
