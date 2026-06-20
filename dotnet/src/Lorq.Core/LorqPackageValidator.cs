using Lorq.Core.PackageValidation;

namespace Lorq.Core;

public static class LorqPackageValidator
{
    public static PackageValidationResult Validate(string packageRoot)
    {
        return new PackageValidationSession(packageRoot).Validate();
    }

    public static MergeInputValidationResult ValidateMergeInputs(IEnumerable<string> shardRoots)
    {
        return new PackageMergeInputValidator(shardRoots).Validate();
    }
}
