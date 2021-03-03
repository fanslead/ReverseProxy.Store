using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class SessionAffinityOptionSetting : KeyValueEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
