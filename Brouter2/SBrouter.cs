using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Brouter2;

public partial class SBrouter : ComponentBase, IDisposable
{

    private static readonly char[] _QueryOrHashStartChar = { '?', '#' };
    private static readonly char[] _Separator = { '/' };


    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public EventHandler<RouteMatchedEventArgs> OnMatch { get; set; }


    [Inject] private NavigationManager _navManager { get; set; }
    [Inject] private INavigationInterception _navInterception { get; set; }

    internal PathInfo PathInfo = new();

    internal List<Route> Routes = new();

    internal void RegisterRoute(Route route) => Routes.Add(route);
    internal void UnregisterRoute(Route route) => Routes.Remove(route);

    private int _notMatchCount = 0;
    internal void Matched(Route route)
    {
        _notMatchCount = 0;
    }
    internal void NotMatched(Route route)
    {
        _notMatchCount--;

        if (_notMatchCount == Routes.Count)
        {
            Console.WriteLine($"Not Found after checking {_notMatchCount} Routes.");
            _notMatchCount = 0;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _navManager.LocationChanged += NavManagerLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        await _navInterception.EnableNavigationInterceptionAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

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

        var seq = 0;
        builder.OpenComponent<CascadingValue<SBrouter>>(seq++);
        builder.AddAttribute(seq++, "Name", "Brouter");
        builder.AddAttribute(seq++, "Value", this);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, ChildContent)));
        builder.CloseComponent();
    }

    private void NavManagerLocationChanged(object sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
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
