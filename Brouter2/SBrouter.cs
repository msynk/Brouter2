using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Data;

namespace Brouter2;

public partial class SBrouter : ComponentBase, IDisposable
{
    private static readonly char[] _QueryOrHashStartChar = { '?', '#' };
    private static readonly char[] _Separator = { '/' };


    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public EventHandler<RouteMatchedEventArgs> OnMatch { get; set; }
    [Parameter] public string NotFound { get; set; }
    [Parameter] public bool EnableParams { get; set; }



    [Inject] private NavigationManager _navManager { get; set; }
    [Inject] private INavigationInterception _navInterception { get; set; }


    internal PathInfo PathInfo = new();


    private readonly List<Route> _routes = new();
    internal void RegisterRoute(Route route) => _routes.Add(route);
    internal void UnregisterRoute(Route route) => _routes.Remove(route);


    private string _location = string.Empty;
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _navManager.LocationChanged += NavManagerLocationChanged;

        _location = _navManager.Uri;

        CreatePathInfo();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        await _navInterception.EnableNavigationInterceptionAsync();

        MatchRoutes();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        var seq = 0;
        builder.OpenComponent<CascadingValue<SBrouter>>(seq++);
        builder.AddAttribute(seq++, "Name", "Brouter");
        builder.AddAttribute(seq++, "Value", this);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, ChildContent)));
        builder.CloseComponent();
    }

    private void NavManagerLocationChanged(object sender, LocationChangedEventArgs e)
    {
        _location = e.Location;

        CreatePathInfo();

        MatchRoutes();
    }




    private void CreatePathInfo()
    {
        var path = _navManager.ToBaseRelativePath(_navManager.Uri);
        var firstIndex = path.IndexOfAny(_QueryOrHashStartChar);
        path = firstIndex < 0 ? path : path.Substring(0, firstIndex);
        path = $"/{path}";
        PathInfo.Path = path;

        var segments = path.Trim('/').Split(_Separator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = Uri.UnescapeDataString(segments[i]);
        }
        PathInfo.Segments = segments;
    }

    private bool _navigated;
    private void MatchRoutes()
    {
        var matchedRoutes = _routes.Select(r => { r.Matched = false; return r; }).Where(Match).ToArray();
        if (matchedRoutes.Length == 0)
        {
            _navManager.NavigateTo(NotFound);
            return;
        }

        _navigated = false;
        foreach (var route in matchedRoutes)
        {
            if (route.Guard is null && route.RedirectTo is not null)
            {
                _navManager.NavigateTo(route.RedirectTo);
                return;
            }

            if (CheckGuard(route))
            {
                route.SetMatched();
            }
            else if (_navigated)
            {
                return;
            }
        }

        UpdateView();
    }

    private void UpdateView()
    {
        //var args = new RouteMatchedEventArgs(_location, _context.Template, _parameters, _context.Route.Content, _context.Route.Component);

        //OnMatch?.Invoke(this, args);

        //if (args.ShouldNotRender) return;

        //_parameters = _context.Parameters;
        //_constraints = _context.Constraints;
        //_currentFragment = _context.Route.Content;
        //_currentComponent = _context.Route.Component;

        StateHasChanged();
    }

    private bool Match(Route route)
    {
        route.Parameters = new Dictionary<string, object>();
        route.Constraints = new Dictionary<string, string[]>();

        // Empty path match all routes
        var routeTemplate = route.RouteTemplate;
        if (string.IsNullOrEmpty(route.RouteTemplate.Template)) return true;

        var segments = PathInfo.Segments;

        if (routeTemplate.TemplateSegments.Length != segments.Length)
        {
            if (routeTemplate.TemplateSegments.Length == 0) return false;

            bool lastSegmentStar = routeTemplate.TemplateSegments[^1].Value == "*" && routeTemplate.TemplateSegments.Length - segments.Length == 1;
            bool lastSegmentDoubleStar = routeTemplate.TemplateSegments[^1].Value == "**" && segments.Length >= routeTemplate.TemplateSegments.Length - 1;

            if (lastSegmentStar is false && lastSegmentDoubleStar is false) return false;
        }

        for (int i = 0; i < routeTemplate.TemplateSegments.Length; i++)
        {
            var templateSegment = routeTemplate.TemplateSegments[i];
            var segment = i < segments.Length ? segments[i] : string.Empty;

            if (templateSegment.TryMatch(segment, out var matchedParameterValue) is false) return false;

            if (templateSegment.IsParameter)
            {
                InitParameters();
                route.Parameters[templateSegment.Value] = matchedParameterValue;
                route.Constraints[templateSegment.Value] = templateSegment.Constraints.Select(rc => rc.Constraint).ToArray();
            }
        }

        return true;

        void InitParameters()
        {
            if (route.Parameters is null)
            {
                route.Parameters = new Dictionary<string, object>();
                route.Constraints = new Dictionary<string, string[]>();
            }
        };
    }

    private bool CheckGuard(Route route)
    {
        var result = true;
        if (route.Guard is not null)
        {
            result = route.Guard.Invoke();
            if (result is false && route.RedirectTo is not null)
            {
                _navManager.NavigateTo(route.RedirectTo);
                _navigated = true;
                return false;
            }
        }

        if (result && route.Parent is not null)
        {
            result = CheckGuard(route.Parent);
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
