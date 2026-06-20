namespace Lorq.Core.PackageValidation;

internal sealed record ShardId
{
    public ShardId(string value)
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
