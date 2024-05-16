namespace ReverseProxy.Store.Entities;

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
    HttpMethodChange = 8,
    RequestHeadersCopy = 9,
    RequestHeaderOriginalHost = 10,
    RequestHeader = 11,
    RequestHeaderRemove = 12,
    RequestHeadersAllowed = 13,
    X_Forwarded = 14,
    Forwarded = 15,
    ClientCert = 16,
    ResponseHeadersCopy = 17,
    ResponseHeader = 18,
    ResponseHeaderRemove = 19,
    ResponseHeadersAllowed = 20,
    ResponseTrailersCopy = 21,
    ResponseTrailer = 22,
    ResponseTrailerRemove = 23,
    ResponseTrailersAllowed = 24,
}
