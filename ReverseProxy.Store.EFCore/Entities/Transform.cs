using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class Transform : KeyValueEntity
    {
        [Key]
        public int Id { get; set; }
        public TransformType Type { get; set; }
    }
    public enum TransformType : int
    {
        PathPrefix = 1,
        PathRemovePrefix = 2,
        PathSet = 3,
        PathPattern = 4,
        QueryValueParameter = 5,
        QueryRouteParameter = 6,
        QueryRemoveParameter = 7,
        HttpMethod = 8,
        RequestHeadersCopy = 9,
        RequestHeaderOriginalHost = 10,
        RequestHeader = 11,
        X_Forwarded = 12,
        Forwarded = 13,
        ClientCert = 14,
        ResponseHeadersCopy = 15,
        ResponseHeader = 16,
        ResponseTrailersCopy = 17,
        ResponseTrailer = 18
    }
}
