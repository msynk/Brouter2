using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

public partial class Route : ComponentBase, IDisposable
{
    internal readonly string Id = Guid.NewGuid().ToString();

    [Parameter] public string Template { get; set; }
    [Parameter] public string RedirectTo { get; set; }
    [Parameter] public Type Component { get; set; }
    [Parameter] public RenderFragment<IDictionary<string, object>> Content { get; set; }
    [Parameter] public Func<bool> Guard { get; set; }
    [Parameter] public Type GuardComponent { get; set; }
    [Parameter] public RenderFragment GuardContent { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }

    [CascadingParameter(Name = "Brouter")] protected SBrouter Brouter { get; set; }
    [CascadingParameter(Name = "ParentRoute")] internal Route Parent { get; set; }

    [CascadingParameter(Name = "RouteParameters")] internal IDictionary<string, object> RouteParameters { get; set; }


    internal string FullTemplate = string.Empty;


    private readonly List<Route> Children = new();
    internal void AddChild(Route route) => Children.Add(route);
    internal void RemoveChild(Route route) => Children.Remove(route);

    internal RouteTemplate RouteTemplate;
    internal IDictionary<string, object> Parameters = null;
    internal IDictionary<string, string[]> Constraints = null;

    private RouteRenderer _renderer;
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Brouter == null)
            throw new InvalidOperationException("A Route must be nested in a Brouter.");

        Brouter.RegisterRoute(this);
        Parent?.AddChild(this);

        FullTemplate = (Parent is null || string.IsNullOrWhiteSpace(Parent.FullTemplate))
                        ? Template
                        : $"{Parent.FullTemplate}/{Template}".Replace("//", "/");

        RouteTemplate = TemplateParser.ParseTemplate(FullTemplate);

        _renderer = new RouteRenderer(this);
    }


    internal bool Matched { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        _renderer.BuildRenderTree(builder, Matched);
    }

    internal void SetMatched()
    {
        Matched = true;

        StateHasChanged();

        if (Parent is null) return;

        Parent.SetMatched();
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Brouter.UnregisterRoute(this);
        Parent?.RemoveChild(this);

        _disposed = true;
    }
}
