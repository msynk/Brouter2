using System.Globalization;

namespace Brouter2;

internal abstract class RouteConstraint
{
    private static readonly IDictionary<string, RouteConstraint> _CachedConstraints = new Dictionary<string, RouteConstraint>();


    public string Constraint { get; private set; }

    public abstract bool TryMatch(string pathSegment, out object convertedValue);


    public static RouteConstraint Parse(string template, string segment, string constraint)
    {
        if (string.IsNullOrEmpty(constraint))
            throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");

        if (_CachedConstraints.TryGetValue(constraint, out var cachedInstance)) return cachedInstance;

        var newInstance = CreateRouteConstraint(constraint);

        if (newInstance is not null)
        {
            _CachedConstraints[constraint] = newInstance;
            newInstance.Constraint = constraint;
            return newInstance;
        }

        throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");
    }

    private static RouteConstraint CreateRouteConstraint(string constraint) => constraint switch
    {
        "int" => new TypeRouteConstraint<int>((string str, out int result) => int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)),
        "bool" => new TypeRouteConstraint<bool>(bool.TryParse),
        "guid" => new TypeRouteConstraint<Guid>(Guid.TryParse),
        "long" => new TypeRouteConstraint<long>((string str, out long result) => long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)),
        "float" => new TypeRouteConstraint<float>((string str, out float result) => float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result)),
        "double" => new TypeRouteConstraint<double>((string str, out double result) => double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result)),
        "decimal" => new TypeRouteConstraint<decimal>((string str, out decimal result) => decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result)),
        "datetime" => new TypeRouteConstraint<DateTime>((string str, out DateTime result) => DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)),
        _ => null
    };
}
