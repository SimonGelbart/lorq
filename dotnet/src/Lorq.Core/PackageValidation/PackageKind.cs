namespace Lorq.Core.PackageValidation;

internal sealed record PackageKind
{
    private const string RunShard = "run_shard";
    private const string MergedExperiment = "merged_experiment";

    public PackageKind(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

    public bool IsSupported()
    {
        return Value is RunShard or MergedExperiment;
    }

    public bool IsMergedExperiment()
    {
        return Value == MergedExperiment;
    }

    public override string ToString()
    {
        return Value;
    }
}
