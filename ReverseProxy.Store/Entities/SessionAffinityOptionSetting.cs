namespace ReverseProxy.Store.Entities;

public class SessionAffinityOptionSetting : KeyValueEntity
{
    [Key]
    public int Id { get; set; }
}
