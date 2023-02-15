using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

public partial class Route : ComponentBase, IDisposable
{
    private static readonly char[] _QueryOrHashStartChar = { '?', '#' };
    private static readonly char[] _Separator = { '/' };

    private IDictionary<string, object> _parameters = null;
    private IDictionary<string, string[]> _constraints = null;
    private RouteTemplate _routeTemplate;
    private string[] _segments;

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


    internal string FullTemplate => (Parent is null || string.IsNullOrWhiteSpace(Parent.FullTemplate))
                                        ? Template
                                        : $"{Parent.FullTemplate}/{Template}".Replace("//", "/");

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Brouter == null)
            throw new InvalidOperationException("A Route must be nested in a Brouter.");

        _navManager.LocationChanged += _navManager_LocationChanged;

        _routeTemplate = TemplateParser.ParseTemplate(FullTemplate);
    }

    private void _navManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        var path = _navManager.ToBaseRelativePath(_navManager.Uri);
        var firstIndex = path.IndexOfAny(_QueryOrHashStartChar);
        path = firstIndex < 0 ? path : path.Substring(0, firstIndex);

        _segments = path.Trim('/').Split(_Separator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < _segments.Length; i++)
        {
            _segments[i] = Uri.UnescapeDataString(_segments[i]);
        }

        if (Match() is false) return;

        new RouteRenderer(this, builder, _parameters, _constraints, Content, Component).BuildRenderTree();
    }

    private void AddRouteParams(RenderTreeBuilder builder, int seq)
    {
        builder.OpenComponent<CascadingValue<IDictionary<string, object>>>(seq++);
        builder.AddAttribute(seq++, "Name", "RouteParameters");
        builder.AddAttribute(seq++, "Value", _parameters);
        if (Content is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, Content)));
        }
        else if (Component is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
            {
                builder2.OpenComponent(seq++, Component);
                builder2.CloseComponent();
            }));
        }
        builder.CloseComponent();
    }




    private bool Match()
    {
        _parameters = null;
        _constraints = null;

        // Empty path match all routes
        if (string.IsNullOrEmpty(_routeTemplate.Template))
        {
            if (CheckGuard() is false) return false;

            if (Guard is null && RedirectTo is not null)
            {
                _navManager.NavigateTo(RedirectTo);
            }

            return true;
        }

        if (_routeTemplate.TemplateSegments.Length != _segments.Length)
        {
            if (_routeTemplate.TemplateSegments.Length == 0) return false;

            bool lastSegmentStar = _routeTemplate.TemplateSegments[^1].Value == "*" && _routeTemplate.TemplateSegments.Length - _segments.Length == 1;
            bool lastSegmentDoubleStar = _routeTemplate.TemplateSegments[^1].Value == "**" && _segments.Length >= _routeTemplate.TemplateSegments.Length - 1;

            if (lastSegmentStar is false && lastSegmentDoubleStar is false) return false;
        }

        for (int i = 0; i < _routeTemplate.TemplateSegments.Length; i++)
        {
            var templateSegment = _routeTemplate.TemplateSegments[i];
            var segment = i < _segments.Length ? _segments[i] : string.Empty;

            if (templateSegment.TryMatch(segment, out var matchedParameterValue) is false) return false;

            if (templateSegment.IsParameter)
            {
                InitParameters();
                _parameters[templateSegment.Value] = matchedParameterValue;
                _constraints[templateSegment.Value] = templateSegment.Constraints.Select(rc => rc.Constraint).ToArray();
            }
        }

        if (CheckGuard() is false) return false;

        if (Guard is null && RedirectTo is not null)
        {
            _navManager.NavigateTo(RedirectTo);
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

        if (Guard is not null)
        {
            result = Guard.Invoke();
            if (result is false && RedirectTo is not null)
            {
                _navManager.NavigateTo(RedirectTo);
            }
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
        if (disposing is false) return;
    }
}
