using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

public class Outlet : ComponentBase, IDisposable
{
    [CascadingParameter(Name = "ParentRoute")] internal Route Parent { get; set; }

    private Route _route;
    private IDictionary<string, object> _parameters;
    internal void Render(Route route, IDictionary<string, object> parameters)
    {
        _route = route;
        _parameters = parameters;
        StateHasChanged();
    }

    protected override Task OnInitializedAsync()
    {
        if (Parent is null)
            throw new InvalidOperationException("An Outlet must be inside a Router.");

        Parent.Outlet = this;


        return base.OnInitializedAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        if (_route is null) return;

        var seq = 0;
        builder.OpenComponent<CascadingValue<Outlet>>(seq++);
        builder.AddAttribute(seq++, "Name", "Outlet");
        builder.AddAttribute(seq++, "Value", this);
        if (_route.Content is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, _route.Content(_parameters))));
        }
        else if (_route.Component is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
            {
                builder2.OpenComponent(seq++, Parent.Component);
                builder2.CloseComponent();
            }));
        }
        builder.CloseComponent();
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

        _route = null;
        Parent.Outlet = null;

        _disposed = true;
    }
}
