namespace Brouter2;

public class RouteMatchedEventArgs
{
    public bool ShouldNotRender { get; set; }

    public string Location { get; }
    public string Template { get; }
    public RenderFragment Content { get; }
    public Type Component { get; }
    public IDictionary<string, object> Parameters { get; }

    public RouteMatchedEventArgs(string location, string template, IDictionary<string, object> parameters, RenderFragment content, Type component)
    {
        Location = location;
        Template = template;
        Parameters = parameters;
        Content = content;
        Component = component;
    }
}
