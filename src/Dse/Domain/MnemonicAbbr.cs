// Copyright (c) PNC Financial Services. All rights reserved.


using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using Vogen;

namespace Dse.Domain;

/// <summary>A three-letter uppercase mnemonic (matches <c>^[A-Z]{3}$</c>), normalized and validated on construction.</summary>
[ValueObject<string>(toPrimitiveCasting: CastOperator.Implicit, comparison: ComparisonGeneration.Omit)]
public readonly partial struct MnemonicAbbr
{
    /// <summary>Regex the canonical (normalized) form satisfies.</summary>
    public const string Pattern = "^[A-Z]{3}$";

    private static string NormalizeInput(string input) => input.Trim().ToUpperInvariant();

    private static Validation Validate(string input) =>
        MnemonicRegex().IsMatch(input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a valid mnemonic (expected {Pattern}).");

    [GeneratedRegex(Pattern)]
    private static partial Regex MnemonicRegex();

    /// <summary>Applies the three-letter-string OpenAPI shape; wired from a schema transformer for this type.</summary>
    public static void ConfigureOpenApiSchema(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.String;
        schema.MinLength = 3;
        schema.MaxLength = 3;
        schema.Pattern = Pattern;
        schema.Description = "A three-letter uppercase mnemonic.";
        schema.Example = JsonValue.Create("DSE");
    }
}
