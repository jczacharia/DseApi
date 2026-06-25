// Copyright (c) PNC Financial Services. All rights reserved.


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dse.Domain;

/// <summary>A three-letter uppercase mnemonic (matches <c>^[A-Z]{3}$</c>), parsed and normalized on construction.</summary>
[JsonConverter(typeof(MnemonicAbbrJsonConverter))]
public readonly record struct MnemonicAbbr : ISpanParsable<MnemonicAbbr>
{
    private readonly char _a;
    private readonly char _b;
    private readonly char _c;

    private MnemonicAbbr(char a, char b, char c) => (_a, _b, _c) = (a, b, c);

    // ISpanParsable / IParsable plumbing (enables generic parsing, model binding, etc.)

    /// <inheritdoc cref="Parse(ReadOnlySpan{char})" />
    public static MnemonicAbbr Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan());

    /// <inheritdoc cref="Parse(ReadOnlySpan{char})" />
    public static MnemonicAbbr Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <summary>Parses and normalizes a mnemonic, throwing if invalid.</summary>
    public static MnemonicAbbr Parse(ReadOnlySpan<char> input) =>
        TryParse(input, out MnemonicAbbr m)
            ? m
            : throw new FormatException($"'{input}' is not a valid mnemonic (expected ^[A-Z]{{3}}$).");

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, out MnemonicAbbr)" />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MnemonicAbbr result) =>
        TryParse(s.AsSpan(), out result);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, out MnemonicAbbr)" />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MnemonicAbbr result) =>
        TryParse(s, out result);

    /// <summary>Attempts to parse and normalize a mnemonic, ignoring surrounding whitespace. Returns <see langword="false" /> if invalid.</summary>
    public static bool TryParse(ReadOnlySpan<char> input, out MnemonicAbbr abbr)
    {
        input = input.Trim();
        if (input.Length == 3 &&
            TryNormalize(input[0], out char a) &&
            TryNormalize(input[1], out char b) &&
            TryNormalize(input[2], out char c))
        {
            abbr = new MnemonicAbbr(a, b, c);
            return true;
        }

        abbr = default;
        return false;
    }

    // ASCII letter check + uppercase, branch-light, culture-independent.
    private static bool TryNormalize(char raw, out char upper)
    {
        upper = (char)(raw & ~0x20); // fold to uppercase
        return (uint)(upper - 'A') <= 'Z' - 'A'; // was it actually a letter?
    }

    /// <summary>Implicitly converts to its normalized three-letter uppercase string.</summary>
    public static implicit operator string(MnemonicAbbr abbr) => abbr.ToString();

    /// <summary>Returns the normalized three-letter uppercase representation.</summary>
    public override string ToString() => string.Create(length: 3, this, static (span, m) =>
    {
        span[0] = m._a;
        span[1] = m._b;
        span[2] = m._c;
    });

    /// <summary>Serializes a <see cref="MnemonicAbbr" /> as its three-letter string form.</summary>
    internal sealed class MnemonicAbbrJsonConverter : JsonConverter<MnemonicAbbr>
    {
        public override MnemonicAbbr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString().AsSpan());

        public override void Write(Utf8JsonWriter writer, MnemonicAbbr value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
