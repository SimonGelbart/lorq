namespace Lorq.Core;

public sealed record LorqPackageJudgeRequest(
    string PackageRoot,
    string JudgeName,
    string FixturePath,
    bool Strict = true);
