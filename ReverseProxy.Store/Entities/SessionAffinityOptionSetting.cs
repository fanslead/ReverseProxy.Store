using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.Entity
{
    public class SessionAffinityOptionSetting : KeyValueEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
