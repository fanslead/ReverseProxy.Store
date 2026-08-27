namespace ReverseProxy.WebApi.Security;

public static class ControlPlaneRoles
{
    public const string Viewer = "Viewer";
    public const string Editor = "Editor";
    public const string Publisher = "Publisher";
    public const string Administrator = "Administrator";

    public static readonly IReadOnlyList<string> All =
    [
        Viewer,
        Editor,
        Publisher,
        Administrator
    ];
}