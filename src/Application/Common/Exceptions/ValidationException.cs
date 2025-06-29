using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace ChatbotApi.Application.Common.Exceptions;

public class ValidationException : Exception
{
    private static JsonSerializerOptions? _jso;

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
        _jso ??= new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(ValidationResult[] validationResults, List<ValidationFailure> failures)
        : this()
    {
        ValidationResults = validationResults;
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public void AddToModelState(ModelStateDictionary dictionary, string? prefix = null)
    {
        if (ValidationResults is { Length: > 0 })
        {
            foreach (ValidationResult result in ValidationResults)
            {
                result.AddToModelState(dictionary, prefix);
            }
        }
        else
        {
            foreach (KeyValuePair<string, string[]> error in Errors)
            {
                foreach (var value in error.Value)
                {
                    dictionary.AddModelError(error.Key, value);
                }
            }
        }
    }

    [JsonIgnore] public ValidationResult[] ValidationResults { get; } = default!;
    public IDictionary<string, string[]> Errors { get; }

    public override string Message => Errors.Any()
        ? JsonSerializer.Serialize(Errors, _jso)
        : base.Message;
}
