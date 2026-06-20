namespace Lorq.Core.PackageValidation;

internal sealed record PackageSchemaVersion
{
    private const int SupportedVersion = 1;

    public PackageSchemaVersion(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public bool IsSupported()
    {
        return Value == SupportedVersion;
    }

    public override string ToString()
    {
        return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
