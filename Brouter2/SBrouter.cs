using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Brouter2;

public partial class SBrouter : ComponentBase, IDisposable
{
    [Inject] private INavigationInterception _navInterception { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public EventHandler<RouteMatchedEventArgs> OnMatch { get; set; }



    protected override void OnInitialized()
    {
        base.OnInitialized();
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

        var seq = 0;
        builder.OpenComponent<CascadingValue<SBrouter>>(seq++);
        builder.AddAttribute(seq++, "Name", "Brouter");
        builder.AddAttribute(seq++, "Value", this);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, ChildContent)));
        builder.CloseComponent();
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
