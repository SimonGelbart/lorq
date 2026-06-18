using System.Globalization;
using System.Text.Json.Nodes;

namespace Lorq.Core;

internal sealed class FakeJudgeFixture
{
    private readonly Dictionary<string, FakeJudgePayload> judgements;

    private FakeJudgeFixture(string schemaVersion, IReadOnlyDictionary<string, FakeJudgePayload> judgements)
    {
        SchemaVersion = schemaVersion;
        this.judgements = new Dictionary<string, FakeJudgePayload>(judgements, StringComparer.Ordinal);
    }

    public string SchemaVersion { get; }

    public bool TryGetPayload(IEnumerable<string> keys, out string matchedKey, out FakeJudgePayload payload)
    {
        foreach (var key in keys)
        {
            if (judgements.TryGetValue(key, out payload!))
            {
                matchedKey = key;
                return true;
            }
        }

        matchedKey = string.Empty;
        payload = FakeJudgePayload.Empty;
        return false;
    }

    public static FakeJudgeFixture Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new LorqPackageFormatException($"Fake judge fixture does not exist: {path}.");
        }

        var parser = new FakeJudgeFixtureParser(File.ReadAllLines(path));
        return parser.Parse();
    }

    private sealed class FakeJudgeFixtureParser
    {
        private readonly IReadOnlyList<string> lines;
        private readonly Dictionary<string, FakeJudgePayload> payloads = new(StringComparer.Ordinal);
        private string schemaVersion = string.Empty;
        private FakeJudgePayloadBuilder? activePayload;
        private string? activeList;
        private string? activeDimension;
        private string? foldedScalar;

        public FakeJudgeFixtureParser(IReadOnlyList<string> lines)
        {
            this.lines = lines;
        }

        public FakeJudgeFixture Parse()
        {
            foreach (var rawLine in lines)
            {
                ParseLine(rawLine);
            }

            AddActivePayload();
            return new FakeJudgeFixture(schemaVersion, payloads);
        }

        private void ParseLine(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine) || rawLine.TrimStart().StartsWith('#'))
            {
                return;
            }

            if (rawLine.StartsWith("schema_version:", StringComparison.Ordinal))
            {
                schemaVersion = ValueAfterColon(rawLine);
                return;
            }

            if (rawLine.StartsWith("- case:", StringComparison.Ordinal))
            {
                AddActivePayload();
                activePayload = new FakeJudgePayloadBuilder { CaseId = ValueAfterColon(rawLine) };
                ResetContext();
                return;
            }

            if (activePayload is null)
            {
                return;
            }

            ParsePayloadLine(rawLine);
        }

        private void ParsePayloadLine(string rawLine)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                AddListValue(trimmed[2..].Trim());
                return;
            }

            if (rawLine.StartsWith("    ", StringComparison.Ordinal) && foldedScalar is not null && !trimmed.Contains(':', StringComparison.Ordinal))
            {
                activePayload!.AppendScalar(foldedScalar, trimmed);
                return;
            }

            if (rawLine.StartsWith("    ", StringComparison.Ordinal) && trimmed.EndsWith(':'))
            {
                activeDimension = trimmed.TrimEnd(':');
                foldedScalar = null;
                return;
            }

            if (rawLine.StartsWith("      ", StringComparison.Ordinal) && activeDimension is not null)
            {
                AddDimensionValue(trimmed);
                return;
            }

            if (!rawLine.StartsWith("  ", StringComparison.Ordinal))
            {
                return;
            }

            AddProperty(trimmed);
        }

        private void AddProperty(string property)
        {
            var separator = property.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
            {
                return;
            }

            var key = property[..separator];
            var value = property[(separator + 1)..].Trim();
            activeDimension = null;
            foldedScalar = null;
            activeList = null;

            switch (key)
            {
                case "mode":
                    activePayload!.ModeId = value;
                    break;
                case "attempt":
                    activePayload!.Attempt = ParseInt(value);
                    break;
                case "ok":
                    activePayload!.Ok = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "overall_score":
                    activePayload!.OverallScore = ParseDecimal(value);
                    break;
                case "confidence":
                    activePayload!.Confidence = value;
                    break;
                case "dimensions":
                    break;
                case "strengths":
                case "weaknesses":
                case "missing_or_questionable":
                    activeList = key;
                    AddInlineList(key, value);
                    break;
                case "summary":
                    activePayload!.Summary = value;
                    foldedScalar = key;
                    break;
            }
        }

        private void AddInlineList(string key, string value)
        {
            if (value == "[]")
            {
                return;
            }

            if (value.Length > 0)
            {
                AddListValue(value);
            }
        }

        private void AddListValue(string value)
        {
            switch (activeList)
            {
                case "strengths":
                    activePayload!.Strengths.Add(value);
                    break;
                case "weaknesses":
                    activePayload!.Weaknesses.Add(value);
                    break;
                case "missing_or_questionable":
                    activePayload!.MissingOrQuestionable.Add(value);
                    break;
            }
        }

        private void AddDimensionValue(string property)
        {
            var separator = property.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
            {
                return;
            }

            var key = property[..separator];
            var value = property[(separator + 1)..].Trim();
            if (key == "score")
            {
                activePayload!.Dimension(activeDimension!).Score = ParseInt(value);
            }

            if (key == "rationale")
            {
                activePayload!.Dimension(activeDimension!).Rationale = value;
            }
        }

        private void AddActivePayload()
        {
            if (activePayload is null)
            {
                return;
            }

            var payload = activePayload.Build();
            payloads[payload.FixtureKey] = payload;
        }

        private void ResetContext()
        {
            activeList = null;
            activeDimension = null;
            foldedScalar = null;
        }

        private static string ValueAfterColon(string rawLine)
        {
            var separator = rawLine.IndexOf(':', StringComparison.Ordinal);
            return separator < 0 ? string.Empty : rawLine[(separator + 1)..].Trim();
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        }

        private static decimal ParseDecimal(string value)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return 0.0m;
            }

            return parsed == decimal.Truncate(parsed)
                ? decimal.Parse(parsed.ToString("0", CultureInfo.InvariantCulture) + ".0", CultureInfo.InvariantCulture)
                : parsed;
        }
    }
}

internal sealed record FakeJudgePayload(
    string CaseId,
    string ModeId,
    int Attempt,
    bool Ok,
    decimal OverallScore,
    string Confidence,
    IReadOnlyDictionary<string, FakeJudgeDimension> Dimensions,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> MissingOrQuestionable,
    string Summary)
{
    public static FakeJudgePayload Empty { get; } = new(
        string.Empty,
        string.Empty,
        0,
        false,
        0m,
        string.Empty,
        new Dictionary<string, FakeJudgeDimension>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        string.Empty);

    public string FixtureKey => $"{CaseId}|{ModeId}|{Attempt}";

    public JsonObject ToQualityJson()
    {
        return new JsonObject
        {
            ["ok"] = Ok,
            ["overall_score"] = JsonValue.Create(OverallScore),
            ["confidence"] = Confidence,
            ["dimensions"] = DimensionsJson(),
            ["strengths"] = StringArray(Strengths),
            ["weaknesses"] = StringArray(Weaknesses),
            ["missing_or_questionable"] = StringArray(MissingOrQuestionable),
            ["summary"] = Summary,
        };
    }

    private JsonObject DimensionsJson()
    {
        var dimensions = new JsonObject();
        foreach (var item in Dimensions)
        {
            dimensions[item.Key] = item.Value.ToJson();
        }

        return dimensions;
    }

    private static JsonArray StringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }
}

internal sealed record FakeJudgeDimension(int Score, string Rationale)
{
    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["score"] = Score,
            ["rationale"] = Rationale,
        };
    }
}

internal sealed class FakeJudgePayloadBuilder
{
    private readonly Dictionary<string, FakeJudgeDimensionBuilder> dimensions = new(StringComparer.Ordinal);

    public string CaseId { get; init; } = string.Empty;
    public string ModeId { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public bool Ok { get; set; }
    public decimal OverallScore { get; set; }
    public string Confidence { get; set; } = string.Empty;
    public List<string> Strengths { get; } = new();
    public List<string> Weaknesses { get; } = new();
    public List<string> MissingOrQuestionable { get; } = new();
    public string Summary { get; set; } = string.Empty;

    public FakeJudgeDimensionBuilder Dimension(string name)
    {
        if (!dimensions.TryGetValue(name, out var dimension))
        {
            dimension = new FakeJudgeDimensionBuilder();
            dimensions[name] = dimension;
        }

        return dimension;
    }

    public void AppendScalar(string key, string value)
    {
        if (key == "summary")
        {
            Summary = string.Join(' ', new[] { Summary, value }.Where(item => item.Length > 0));
        }
    }

    public FakeJudgePayload Build()
    {
        return new FakeJudgePayload(
            CaseId,
            ModeId,
            Attempt,
            Ok,
            OverallScore,
            Confidence,
            dimensions.ToDictionary(item => item.Key, item => item.Value.Build(), StringComparer.Ordinal),
            Strengths.ToArray(),
            Weaknesses.ToArray(),
            MissingOrQuestionable.ToArray(),
            Summary);
    }
}

internal sealed class FakeJudgeDimensionBuilder
{
    public int Score { get; set; }
    public string Rationale { get; set; } = string.Empty;

    public FakeJudgeDimension Build()
    {
        return new FakeJudgeDimension(Score, Rationale);
    }
}
