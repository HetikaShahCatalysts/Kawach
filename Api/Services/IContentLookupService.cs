namespace Api.Services;

public interface IContentLookupService
{
    NormalizedContent Resolve(
        string languageCode,
        string stepCode,
        string stepName,
        string moduleCode,
        string moduleName,
        string questionCode,
        string questionText,
        string? answerCode,
        string answerText,
        decimal submittedScore);

    string ResolveStep(string languageCode, string stepCode, string stepName);
}

public sealed record NormalizedContent(
    string StepEnglish,
    string ModuleEnglish,
    string QuestionEnglish,
    string AnswerEnglish,
    decimal Score);
