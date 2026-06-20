namespace Lorq.Cli.Commands.Parsing;

/// <summary>
/// Parses command-specific argument lists into typed option records.
/// </summary>
public static class LorqCommandOptionsParser
{

    public static ParseResult<RunOptions> ParseRun(IReadOnlyList<string> values)
    {
        string? outputRoot = null;
        string suiteRoot = ".";
        string? shardId = null;
        string packageId = "deterministic-benchmark";
        string? benchmarkPath = null;
        string? adapterFixturePath = null;
        string? adapterCommand = null;
        string? adapterWorkingDirectory = null;
        string? adapterProfile = null;
        string? codexCommand = null;
        string? workRoot = null;
        var adapterArguments = new List<string>();
        var codexArguments = new List<string>();
        var noJudge = false;

        for (var index = 0; index < values.Count; index++)
        {
            index = ParseRunValue(values, index, ref outputRoot, ref suiteRoot, ref shardId, ref packageId, ref benchmarkPath, ref adapterFixturePath, ref adapterCommand, adapterArguments, ref adapterWorkingDirectory, ref adapterProfile, ref codexCommand, codexArguments, ref workRoot, ref noJudge);
        }

        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            return ParseResult<RunOptions>.Failure("run --no-judge requires --out <run-shard-root>.");
        }

        if (!noJudge)
        {
            return ParseResult<RunOptions>.Failure("Only run --no-judge is implemented in this migration slice.");
        }

        if (!string.IsNullOrWhiteSpace(adapterProfile) && string.IsNullOrWhiteSpace(adapterCommand))
        {
            return ParseResult<RunOptions>.Failure("run --adapter-profile requires --adapter-command <wrapper>.");
        }

        shardId ??= Path.GetFileName(Path.TrimEndingDirectorySeparator(outputRoot));
        benchmarkPath ??= "benchmark.yaml";
        adapterFixturePath ??= Path.Combine("fixtures", "fake-agent.yaml");
        return ParseResult<RunOptions>.Success(new RunOptions(outputRoot, suiteRoot, shardId, packageId, benchmarkPath, adapterFixturePath, noJudge, adapterCommand, adapterArguments, adapterWorkingDirectory, adapterProfile, codexCommand, codexArguments, workRoot));
    }


    public static ParseResult<AdapterConformanceOptions> ParseAdapterConformance(IReadOnlyList<string> values)
    {
        string? adapterCommand = null;
        string? adapterWorkingDirectory = null;
        string? outputRoot = null;
        var timeoutMilliseconds = 30000;
        var adapterArguments = new List<string>();

        for (var index = 0; index < values.Count; index++)
        {
            index = ParseAdapterConformanceValue(values, index, ref adapterCommand, adapterArguments, ref adapterWorkingDirectory, ref outputRoot, ref timeoutMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(adapterCommand) || string.IsNullOrWhiteSpace(outputRoot))
        {
            return ParseResult<AdapterConformanceOptions>.Failure("adapter-conformance requires --adapter-command <executable> and --out <output-root>.");
        }

        if (timeoutMilliseconds <= 0)
        {
            return ParseResult<AdapterConformanceOptions>.Failure("adapter-conformance requires --timeout-ms to be greater than zero.");
        }

        return ParseResult<AdapterConformanceOptions>.Success(new AdapterConformanceOptions(adapterCommand, adapterArguments, adapterWorkingDirectory, outputRoot, timeoutMilliseconds));
    }

    public static ParseResult<ValidatePackageOptions> ParseValidatePackage(IReadOnlyList<string> values)
    {
        if (values.Count < 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            return ParseResult<ValidatePackageOptions>.Failure("validate-package requires a package root.");
        }

        return ParseResult<ValidatePackageOptions>.Success(new ValidatePackageOptions(values[0]));
    }

    public static ParseResult<ValidateMergeInputsOptions> ParseValidateMergeInputs(IReadOnlyList<string> values)
    {
        if (values.Count < 2)
        {
            return ParseResult<ValidateMergeInputsOptions>.Failure("validate-merge-inputs requires at least two shard roots.");
        }

        return ParseResult<ValidateMergeInputsOptions>.Success(new ValidateMergeInputsOptions(values.ToArray()));
    }

    public static ParseResult<RebuildIndexesOptions> ParseRebuildIndexes(IReadOnlyList<string> values)
    {
        if (values.Count < 2)
        {
            return ParseResult<RebuildIndexesOptions>.Failure("rebuild-indexes requires a source package root and target root.");
        }

        return ParseResult<RebuildIndexesOptions>.Success(new RebuildIndexesOptions(values[0], values[1]));
    }

    public static ParseResult<MergeShardsOptions> ParseMergeShards(IReadOnlyList<string> values)
    {
        var shardRoots = new List<string>();
        string? outputRoot = null;
        string? packageId = null;
        string? benchmarkPath = null;
        var strict = true;

        for (var index = 0; index < values.Count; index++)
        {
            index = ParseMergeValue(values, index, shardRoots, ref outputRoot, ref packageId, ref benchmarkPath, ref strict);
        }

        if (shardRoots.Count == 0 || string.IsNullOrWhiteSpace(outputRoot) || string.IsNullOrWhiteSpace(packageId))
        {
            return ParseResult<MergeShardsOptions>.Failure("merge-shards requires at least one shard root plus --out <package-root> and --package-id <id>.");
        }

        return ParseResult<MergeShardsOptions>.Success(new MergeShardsOptions(shardRoots, outputRoot, packageId, benchmarkPath, strict));
    }

    public static ParseResult<JudgePackageOptions> ParseJudgePackage(IReadOnlyList<string> values)
    {
        string? packageRoot = null;
        string? judgeName = null;
        string? fixturePath = null;
        var strict = true;

        for (var index = 0; index < values.Count; index++)
        {
            index = ParseJudgeValue(values, index, ref packageRoot, ref judgeName, ref fixturePath, ref strict);
        }

        if (string.IsNullOrWhiteSpace(packageRoot) || string.IsNullOrWhiteSpace(judgeName) || string.IsNullOrWhiteSpace(fixturePath))
        {
            return ParseResult<JudgePackageOptions>.Failure("judge-package requires <package-root>, --name <judge-name>, and --fixture <path>.");
        }

        return ParseResult<JudgePackageOptions>.Success(new JudgePackageOptions(packageRoot, judgeName, fixturePath, strict));
    }

    public static ParseResult<ReportPackageOptions> ParseReportPackage(IReadOnlyList<string> values)
    {
        string? packageRoot = null;
        var primaryJudgement = "judge-primary";

        for (var index = 0; index < values.Count; index++)
        {
            index = ParseReportValue(values, index, ref packageRoot, ref primaryJudgement);
        }

        if (string.IsNullOrWhiteSpace(packageRoot) || string.IsNullOrWhiteSpace(primaryJudgement))
        {
            return ParseResult<ReportPackageOptions>.Failure("report-package requires <package-root> and optionally --primary-judgement <judge-name>.");
        }

        return ParseResult<ReportPackageOptions>.Success(new ReportPackageOptions(packageRoot, primaryJudgement));
    }



    private static int ParseAdapterConformanceValue(
        IReadOnlyList<string> values,
        int index,
        ref string? adapterCommand,
        List<string> adapterArguments,
        ref string? adapterWorkingDirectory,
        ref string? outputRoot,
        ref int timeoutMilliseconds)
    {
        var value = values[index];
        switch (value)
        {
            case "--adapter-command" when index + 1 < values.Count:
                adapterCommand = values[index + 1];
                return index + 1;
            case "--adapter-arg" when index + 1 < values.Count:
                adapterArguments.Add(values[index + 1]);
                return index + 1;
            case "--adapter-working-directory" when index + 1 < values.Count:
                adapterWorkingDirectory = values[index + 1];
                return index + 1;
            case "--out" when index + 1 < values.Count:
                outputRoot = values[index + 1];
                return index + 1;
            case "--timeout-ms" when index + 1 < values.Count && int.TryParse(values[index + 1], out var parsed):
                timeoutMilliseconds = parsed;
                return index + 1;
            default:
                return index;
        }
    }

    private static int ParseRunValue(
        IReadOnlyList<string> values,
        int index,
        ref string? outputRoot,
        ref string suiteRoot,
        ref string? shardId,
        ref string packageId,
        ref string? benchmarkPath,
        ref string? adapterFixturePath,
        ref string? adapterCommand,
        List<string> adapterArguments,
        ref string? adapterWorkingDirectory,
        ref string? adapterProfile,
        ref string? codexCommand,
        List<string> codexArguments,
        ref string? workRoot,
        ref bool noJudge)
    {
        var value = values[index];
        switch (value)
        {
            case "--no-judge":
                noJudge = true;
                return index;
            case "--out" when index + 1 < values.Count:
                outputRoot = values[index + 1];
                return index + 1;
            case "--suite-root" when index + 1 < values.Count:
                suiteRoot = values[index + 1];
                return index + 1;
            case "--shard-id" when index + 1 < values.Count:
                shardId = values[index + 1];
                return index + 1;
            case "--package-id" when index + 1 < values.Count:
                packageId = values[index + 1];
                return index + 1;
            case "--benchmark" when index + 1 < values.Count:
                benchmarkPath = values[index + 1];
                return index + 1;
            case "--adapter-fixture" when index + 1 < values.Count:
                adapterFixturePath = values[index + 1];
                return index + 1;
            case "--adapter-command" when index + 1 < values.Count:
                adapterCommand = values[index + 1];
                return index + 1;
            case "--adapter-arg" when index + 1 < values.Count:
                adapterArguments.Add(values[index + 1]);
                return index + 1;
            case "--adapter-working-directory" when index + 1 < values.Count:
                adapterWorkingDirectory = values[index + 1];
                return index + 1;
            case "--adapter-profile" when index + 1 < values.Count:
                adapterProfile = values[index + 1];
                return index + 1;
            case "--codex-command" when index + 1 < values.Count:
                codexCommand = values[index + 1];
                return index + 1;
            case "--codex-arg" when index + 1 < values.Count:
                codexArguments.Add(values[index + 1]);
                return index + 1;
            case "--work-root" when index + 1 < values.Count:
                workRoot = values[index + 1];
                return index + 1;
            default:
                return index;
        }
    }

    private static int ParseMergeValue(
        IReadOnlyList<string> values,
        int index,
        List<string> shardRoots,
        ref string? outputRoot,
        ref string? packageId,
        ref string? benchmarkPath,
        ref bool strict)
    {
        var value = values[index];
        switch (value)
        {
            case "--out" when index + 1 < values.Count:
                outputRoot = values[index + 1];
                return index + 1;
            case "--package-id" when index + 1 < values.Count:
                packageId = values[index + 1];
                return index + 1;
            case "--benchmark" when index + 1 < values.Count:
                benchmarkPath = values[index + 1];
                return index + 1;
            case "--allow-incompatible":
                strict = false;
                return index;
            default:
                shardRoots.Add(value);
                return index;
        }
    }

    private static int ParseJudgeValue(
        IReadOnlyList<string> values,
        int index,
        ref string? packageRoot,
        ref string? judgeName,
        ref string? fixturePath,
        ref bool strict)
    {
        var value = values[index];
        switch (value)
        {
            case "--name" when index + 1 < values.Count:
                judgeName = values[index + 1];
                return index + 1;
            case "--fixture" when index + 1 < values.Count:
                fixturePath = values[index + 1];
                return index + 1;
            case "--allow-missing-fixtures":
                strict = false;
                return index;
            default:
                packageRoot ??= value;
                return index;
        }
    }

    private static int ParseReportValue(IReadOnlyList<string> values, int index, ref string? packageRoot, ref string primaryJudgement)
    {
        var value = values[index];
        switch (value)
        {
            case "--primary-judgement" when index + 1 < values.Count:
                primaryJudgement = values[index + 1];
                return index + 1;
            default:
                packageRoot ??= value;
                return index;
        }
    }
}
