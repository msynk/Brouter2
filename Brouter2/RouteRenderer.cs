using Microsoft.AspNetCore.Components.Rendering;

namespace Brouter2;

internal class RouteRenderer
{
    private readonly Route _route;
    private readonly RenderTreeBuilder _builder;
    private readonly RenderFragment _currentFragment;
    private readonly Type _currentComponent;
    private readonly IDictionary<string, object> _parameters;
    private readonly IDictionary<string, string[]> _constraints;

    public RouteRenderer(Route route,
                           RenderTreeBuilder builder,
                           IDictionary<string, object> parameters,
                           IDictionary<string, string[]> constraints,
                           RenderFragment currentFragment,
                           Type currentComponent)
    {
        _route = route;
        _builder = builder;
        _parameters = parameters;
        _constraints = constraints;
        _currentFragment = currentFragment;
        _currentComponent = currentComponent;
    }

    public void BuildRenderTree()
    {
        var seq = CreateRouteCascadingValue();
        CreateParametersCascadingValues(++seq);
    }

    private int CreateRouteCascadingValue()
    {
        var seq = 0;
        _builder.OpenComponent<CascadingValue<Route>>(seq++);
        _builder.AddAttribute(seq++, "Name", "ParentRoute");
        _builder.AddAttribute(seq++, "Value", _route);
        _builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, _route.ChildContent)));
        _builder.CloseComponent();
        return seq;
    }

    private void CreateParametersCascadingValues(int seq)
    {
        if (_parameters is null || _parameters.Count == 0)
        {
            AddRouteParams(_builder, seq);
            return;
        };

        RecursiveCreate(_builder, 0, seq, _parameters.ToArray());
    }

    private void RecursiveCreate(RenderTreeBuilder builder, int idx, int seq, KeyValuePair<string, object>[] arr)
    {
        var p = arr[idx];

        seq = AddComponent(builder, p.Key, p.Value, seq);

        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
        {
            if (++idx == arr.Length)
            {
                AddRouteParams(builder2, seq);
                return;
            }

            RecursiveCreate(builder2, idx, seq, arr);
        }));

        builder.CloseComponent();
    }

    private void AddRouteParams(RenderTreeBuilder builder, int seq)
    {
        builder.OpenComponent<CascadingValue<IDictionary<string, object>>>(seq++);
        builder.AddAttribute(seq++, "Name", "RouteParameters");
        builder.AddAttribute(seq++, "Value", _parameters);
        if (_currentFragment is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 => builder2.AddContent(seq, _currentFragment)));
        }
        else if (_currentComponent is not null)
        {
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(builder2 =>
            {
                builder2.OpenComponent(seq++, _currentComponent);
                builder2.CloseComponent();
            }));
        }
        builder.CloseComponent();
    }

    private int AddComponent<T>(RenderTreeBuilder builder, string name, T value, int seq)
    {
        var constraints = _constraints[name];
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
