namespace Lorq.Core.PackageValidation;

internal sealed record PackageId
{
    public PackageId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}
