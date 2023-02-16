using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

public partial class Route : ComponentBase, IDisposable
{
    private IDictionary<string, object> _parameters = null;
    private IDictionary<string, string[]> _constraints = null;
    private RouteTemplate _routeTemplate;
    private bool _disposed;

    internal readonly string Id = Guid.NewGuid().ToString();

    [Parameter] public string Template { get; set; }
    [Parameter] public string RedirectTo { get; set; }
    [Parameter] public Type Component { get; set; }
    [Parameter] public RenderFragment Content { get; set; }
    [Parameter] public Func<bool> Guard { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }

    [CascadingParameter(Name = "Brouter")] protected SBrouter Brouter { get; set; }
    [CascadingParameter(Name = "ParentRoute")] internal Route Parent { get; set; }


    [Inject] private NavigationManager _navManager { get; set; }


    internal string FullTemplate = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Brouter == null)
            throw new InvalidOperationException("A Route must be nested in a Brouter.");

        Brouter.RegisterRoute(this);

        FullTemplate = (Parent is null || string.IsNullOrWhiteSpace(Parent.FullTemplate))
                        ? Template
                        : $"{Parent.FullTemplate}/{Template}".Replace("//", "/");

        _routeTemplate = TemplateParser.ParseTemplate(FullTemplate);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        if (Match() is false && Brouter.PathInfo.Path.StartsWith(FullTemplate) is false)
        {
            Brouter.NotMatched(this);
            return;
        }

        Brouter.Matched(this);

        if (CheckGuard() is false) return;

        new RouteRenderer(this, builder, _parameters, _constraints, Content, Component).BuildRenderTree();
    }

    private bool Match()
    {
        _parameters = null;
        _constraints = null;

        // Empty path match all routes
        if (string.IsNullOrEmpty(_routeTemplate.Template))
        {
            if (Guard is null && RedirectTo is not null)
            {
                _navManager.NavigateTo(RedirectTo);
                return false;
            }

            return true;
        }

        var segments = Brouter.PathInfo.Segments;

        if (_routeTemplate.TemplateSegments.Length != segments.Length)
        {
            if (_routeTemplate.TemplateSegments.Length == 0) return false;

            bool lastSegmentStar = _routeTemplate.TemplateSegments[^1].Value == "*" && _routeTemplate.TemplateSegments.Length - segments.Length == 1;
            bool lastSegmentDoubleStar = _routeTemplate.TemplateSegments[^1].Value == "**" && segments.Length >= _routeTemplate.TemplateSegments.Length - 1;

            if (lastSegmentStar is false && lastSegmentDoubleStar is false) return false;
        }

        for (int i = 0; i < _routeTemplate.TemplateSegments.Length; i++)
        {
            var templateSegment = _routeTemplate.TemplateSegments[i];
            var segment = i < segments.Length ? segments[i] : string.Empty;

            if (templateSegment.TryMatch(segment, out var matchedParameterValue) is false) return false;

            if (templateSegment.IsParameter)
            {
                InitParameters();
                _parameters[templateSegment.Value] = matchedParameterValue;
                _constraints[templateSegment.Value] = templateSegment.Constraints.Select(rc => rc.Constraint).ToArray();
            }
        }

        if (Guard is null && RedirectTo is not null)
        {
            _navManager.NavigateTo(RedirectTo);
            return false;
        }

        return true;

        void InitParameters()
        {
            if (_parameters is null)
            {
                _parameters = new Dictionary<string, object>();
                _constraints = new Dictionary<string, string[]>();
            }
        };
    }

    private bool CheckGuard()
    {
        var result = true;
        if (Guard is null) return true;

        result = Guard.Invoke();
        if (result is false && RedirectTo is not null)
        {
            _navManager.NavigateTo(RedirectTo);
            return false;
        }

        return result;
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Brouter.UnregisterRoute(this);

        _disposed = true;
    }
}
