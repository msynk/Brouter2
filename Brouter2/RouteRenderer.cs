using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

internal class RouteRenderer
{
    private readonly Route _route;

    public RouteRenderer(Route route)
    {
        _route = route;
    }

    public void BuildRenderTree(RenderTreeBuilder builder, bool matched)
    {
        var seq = 0;
        builder.OpenComponent<CascadingValue<Route>>(seq++);
        builder.AddAttribute(seq++, "Name", "ParentRoute");
        builder.AddAttribute(seq++, "Value", _route);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
        {
            builder2.AddContent(seq, _route.ChildContent);
            if (matched)
            {
                if (_route.Parameters is null || _route.Parameters.Count == 0)
                {
                    AddRouteParameters(builder2, seq);
                }
                else
                {
                    AddRecursiveParameters(builder2, 0, seq, _route.Parameters.ToArray());
                }
            }
        }));
        builder.CloseComponent();
    }

    private void AddRecursiveParameters(RenderTreeBuilder builder, int idx, int seq, KeyValuePair<string, object>[] parameters)
    {
        var parameter = parameters[idx];

        seq = AddComponent(builder, parameter.Key, parameter.Value, seq);

        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
        {
            if (++idx == parameters.Length)
            {
                AddRouteParameters(builder2, seq);
                return;
            }

            AddRecursiveParameters(builder2, idx, seq, parameters);
        }));

        builder.CloseComponent();
    }

    private void AddRouteParameters(RenderTreeBuilder builder, int seq)
    {
        builder.OpenComponent<CascadingValue<IDictionary<string, object>>>(seq++);
        builder.AddAttribute(seq++, "Name", "RouteParameters");
        var routeParams = MergeParameters(_route.RouteParameters, _route.Parameters);
        builder.AddAttribute(seq++, "Value", routeParams);
        if (_route.Content is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, _route.Content(routeParams))));
        }
        else if (_route.Component is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
            {
                builder2.OpenComponent(seq++, _route.Component);
                builder2.CloseComponent();
            }));
        }
        builder.CloseComponent();
    }

    private static IDictionary<string, object> MergeParameters(IDictionary<string, object> routeParameters, IDictionary<string, object> parameters)
    {
        return parameters;
    }

    private int AddComponent<T>(RenderTreeBuilder builder, string name, T value, int seq)
    {
        var constraints = _route.Constraints[name];
        if (constraints is null || constraints.Length == 0)
        {
            builder.OpenComponent<CascadingValue<T>>(seq++);
        }
        else
        {
            if (OpenConstrainedComp(builder, constraints, value, seq++) is false)
            {
                builder.OpenComponent<CascadingValue<T>>(seq);
            }
            //builder.OpenComponent<CascadingValue<int>>(seq++);
        }
        builder.AddAttribute(seq++, "Name", name);
        builder.AddAttribute(seq++, "Value", value);
        return seq;
    }

    private static bool OpenConstrainedComp(RenderTreeBuilder builder, string[] constraints, object value, int seq)
    {
        //foreach (var constraint in constraints)
        //{
        //    if (CheckConstraint(constraint, value))
        //    {
        //        OpenComp(builder, constraint, seq);
        //        return true;
        //    }
        //}
        //return false;

        OpenComp(builder, constraints[^1], seq);

        return true;
    }

    private static bool CheckConstraint(string constraint, object value)
    {
        return constraint switch
        {
            "int" => int.TryParse(value.ToString(), out int result),
            "bool" => bool.TryParse(value.ToString(), out bool result),
            "guid" => Guid.TryParse(value.ToString(), out Guid result),
            "long" => long.TryParse(value.ToString(), out long result),
            "float" => float.TryParse(value.ToString(), out float result),
            "double" => double.TryParse(value.ToString(), out double result),
            "decimal" => decimal.TryParse(value.ToString(), out decimal result),
            "datetime" => DateTime.TryParse(value.ToString(), out DateTime result),
            _ => false
        };
    }

    private static void OpenComp(RenderTreeBuilder builder, string constraint, int seq)
    {
        (constraint switch
        {
            "int" => (Action)(() => builder.OpenComponent<CascadingValue<int>>(seq)),
            "bool" => () => builder.OpenComponent<CascadingValue<bool>>(seq),
            "guid" => () => builder.OpenComponent<CascadingValue<Guid>>(seq),
            "long" => () => builder.OpenComponent<CascadingValue<long>>(seq),
            "float" => () => builder.OpenComponent<CascadingValue<float>>(seq),
            "double" => () => builder.OpenComponent<CascadingValue<double>>(seq),
            "decimal" => () => builder.OpenComponent<CascadingValue<decimal>>(seq),
            "datetime" => () => builder.OpenComponent<CascadingValue<DateTime>>(seq),
            _ => () => { }
        })();
    }
}
