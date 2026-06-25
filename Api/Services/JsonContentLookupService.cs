using System.Text.Json;

namespace Api.Services;

public sealed class JsonContentLookupService(IWebHostEnvironment environment) : IContentLookupService
{
    private readonly string _lookupPath =
        Path.Combine(environment.ContentRootPath, "Content", "content-lookup.json");

    public NormalizedContent Resolve(
        string languageCode,
        string stepCode,
        string stepName,
        string moduleCode,
        string moduleName,
        string questionCode,
        string questionText,
        string? answerCode,
        string answerText,
        decimal submittedScore)
    {
        if (!IsHindi(languageCode))
        {
            return new NormalizedContent(
                stepName.Trim(),
                moduleName.Trim(),
                questionText.Trim(),
                answerText.Trim(),
                submittedScore);
        }

        var lookup = LoadLookup();
        var step = ResolveValue(lookup.Steps, stepCode, stepName, "step", true);
        var module = ResolveValue(lookup.Modules, moduleCode, moduleName, "module", true);
        var question = ResolveValue(
            lookup.Questions,
            questionCode,
            questionText,
            "question",
            true);
        var answer = ResolveAnswer(
            lookup,
            questionCode,
            answerCode,
            answerText,
            true);

        return new NormalizedContent(
            step.English,
            module.English,
            question.English,
            answer.English,
            answer.Score);
    }

    public string ResolveStep(string languageCode, string stepCode, string stepName)
    {
        if (!IsHindi(languageCode))
        {
            return stepName.Trim();
        }

        return ResolveValue(LoadLookup().Steps, stepCode, stepName, "step", true).English;
    }

    private ContentLookup LoadLookup()
    {
        if (!File.Exists(_lookupPath))
        {
            throw new ContentLookupException(
                $"Hindi content lookup file was not found at '{_lookupPath}'.");
        }

        try
        {
            using var stream = File.OpenRead(_lookupPath);
            return JsonSerializer.Deserialize<ContentLookup>(
                       stream,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new ContentLookup();
        }
        catch (JsonException exception)
        {
            throw new ContentLookupException("Hindi content lookup file is invalid.", exception);
        }
    }

    private static ContentValue ResolveValue(
        IReadOnlyDictionary<string, ContentValue> values,
        string code,
        string submittedText,
        string contentType,
        bool isHindi)
    {
        if (values.TryGetValue(code, out var value) &&
            MatchesSubmittedText(
                isHindi ? value.Hindi : [value.English],
                submittedText))
        {
            return value;
        }

        throw new ContentLookupException(
            $"No verified English {contentType} mapping exists for '{code}'.");
    }

    private static ContentValue ResolveAnswer(
        ContentLookup lookup,
        string questionCode,
        string? answerCode,
        string submittedText,
        bool isHindi)
    {
        var lookupKey = string.IsNullOrWhiteSpace(answerCode)
            ? null
            : $"{questionCode}:{answerCode}";

        if (lookupKey is not null &&
            lookup.Answers.TryGetValue(lookupKey, out var answer) &&
            MatchesSubmittedText(
                isHindi ? answer.Hindi : [answer.English],
                submittedText))
        {
            return answer;
        }

        throw new ContentLookupException(
            $"No verified English answer mapping exists for '{lookupKey ?? questionCode}'.");
    }

    private static bool MatchesSubmittedText(IEnumerable<string> configuredValues, string submittedText) =>
        configuredValues.Any(value =>
            string.Equals(value.Trim(), submittedText.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsHindi(string languageCode) =>
        languageCode.Equals("hi", StringComparison.OrdinalIgnoreCase) ||
        languageCode.StartsWith("hi-", StringComparison.OrdinalIgnoreCase);

    private sealed class ContentLookup
    {
        public Dictionary<string, ContentValue> Steps { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ContentValue> Modules { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ContentValue> Questions { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ContentValue> Answers { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ContentValue
    {
        public string English { get; init; } = string.Empty;
        public string[] Hindi { get; init; } = [];
        public decimal Score { get; init; }
    }
}

public sealed class ContentLookupException : Exception
{
    public ContentLookupException(string message) : base(message)
    {
    }

    public ContentLookupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
