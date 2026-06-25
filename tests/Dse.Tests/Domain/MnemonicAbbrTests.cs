// Copyright (c) PNC Financial Services. All rights reserved.


using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Dse.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace Dse.Tests.Domain;

public sealed class MnemonicAbbrTests(ITestOutputHelper outputHelper) : ApiTest(outputHelper)
{
    [Fact]
    public async Task ShouldWorkInEndpoints()
    {
        await using ApiHost host = ApiHost.WithExtender(Out, app =>
        {
            app.MapGet("/mne/{mne}", (MnemonicAbbr mne) =>
            {
                Assert.Equal(mne, MnemonicAbbr.From("DSE"));
                return TypedResults.Json(new { mne });
            });
        });
        var response = await host.CreateClient()
            .GetFromJsonAsync<Dictionary<string, string>>("/mne/dSe", TestContext.Current.CancellationToken);
        Assert.Equal("DSE", response?["mne"]);
    }
}

public sealed class MnemonicAbbrParseTests
{
    [Theory]
    [InlineData("DSE", "DSE")] // already canonical
    [InlineData("dse", "DSE")] // all lower
    [InlineData("dSe", "DSE")] // mixed
    [InlineData("AbC", "ABC")]
    [InlineData("xyz", "XYZ")] // end of the alphabet uppercases correctly
    [InlineData("   dSe ", "DSE")] // surrounding whitespace trimmed
    [InlineData("\tDSE\n", "DSE")] // tabs and newlines trimmed too
    [InlineData(" abc ", "ABC")]
    public void From_ValidInput_NormalizesToUppercase(string input, string expected) =>
        Assert.Equal(expected, MnemonicAbbr.From(input).Value);

    [Theory]
    [InlineData("")] // empty
    [InlineData("D")] // too short
    [InlineData("DS")] // too short
    [InlineData("DSEE")] // too long
    [InlineData("DS1")] // digit
    [InlineData("DS ")] // trims to "DS" — too short
    [InlineData("DS-")] // punctuation
    [InlineData("D[E")] // non-letter
    [InlineData("D0E")] // digit
    [InlineData("ÀBC")] // non-ASCII letter
    [InlineData("DÉE")] // accented char in the middle
    [InlineData("D E")] // inner whitespace is not stripped
    [InlineData("D\tE")]
    public void TryParse_InvalidInput_ReturnsFalse(string input) =>
        Assert.False(MnemonicAbbr.TryParse(input, provider: null, out _));

    [Fact]
    public void TryParse_NullString_ReturnsFalse() =>
        Assert.False(MnemonicAbbr.TryParse(s: null, provider: null, out _));

    [Fact]
    public void Parse_TrimsAndNormalizes() =>
        Assert.Equal("DSE", MnemonicAbbr.Parse("   dSe ", provider: null).Value);

    [Fact]
    public void From_InvalidInput_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => MnemonicAbbr.From("nope"));
        Assert.Contains("^[A-Z]{3}$", ex.Message);
    }

    [Fact]
    public void ImplicitStringConversion_YieldsNormalizedString()
    {
        string s = MnemonicAbbr.From("dSe");
        Assert.Equal("DSE", s);
    }

    [Fact]
    public void ImplicitStringConversion_FlowsToStringParameter()
    {
        static string Echo(string value) => value;
        Assert.Equal("DSE", Echo(MnemonicAbbr.From("dSe")));
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByNormalization()
    {
        MnemonicAbbr lower = MnemonicAbbr.From("dse");
        MnemonicAbbr upper = MnemonicAbbr.From("DSE");

        Assert.Equal(upper, lower);
        Assert.True(upper == lower);
        Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
    }

    [Fact]
    public void Equality_DistinctValues_AreNotEqual()
    {
        Assert.NotEqual(MnemonicAbbr.From("DSE"), MnemonicAbbr.From("DSF"));
        Assert.True(MnemonicAbbr.From("DSE") != MnemonicAbbr.From("DSF"));
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey()
    {
        var map = new Dictionary<MnemonicAbbr, int> { [MnemonicAbbr.From("DSE")] = 42 };
        Assert.Equal(expected: 42, map[MnemonicAbbr.From("dse")]);
    }

    [Fact]
    public void ISpanParsable_RoundTrips()
    {
        Assert.True(MnemonicAbbr.TryParse("dSe".AsSpan(), provider: null, out MnemonicAbbr viaTryParse));
        Assert.Equal(MnemonicAbbr.Parse("dSe".AsSpan(), provider: null), viaTryParse);
        Assert.Equal("DSE", viaTryParse.Value);
    }

    [Theory]
    [InlineData("dSe", "DSE")]
    [InlineData("DSE", "DSE")]
    public void Json_DeserializesAndNormalizes(string json, string expected)
    {
        var abbr = JsonSerializer.Deserialize<MnemonicAbbr>($"\"{json}\"");
        Assert.Equal(MnemonicAbbr.From(expected), abbr);
    }

    [Fact]
    public void Json_Serializes_AsNormalizedString() =>
        Assert.Equal("\"DSE\"", JsonSerializer.Serialize(MnemonicAbbr.From("dSe")));

    [Fact]
    public void Json_RoundTrips()
    {
        MnemonicAbbr original = MnemonicAbbr.From("dSe");
        string json = JsonSerializer.Serialize(original);
        Assert.Equal(original, JsonSerializer.Deserialize<MnemonicAbbr>(json));
    }

    [Fact]
    public void Json_InvalidString_ThrowsFormatException() =>
        Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<MnemonicAbbr>("\"nope\""));

    // MVC binds simple route/query params via TypeDescriptor.GetConverter(...).ConvertFrom(string).
    [Theory]
    [InlineData("dSe", "DSE")]
    [InlineData("   abc ", "ABC")] // trimmed, like every other entry point
    public void TypeConverter_ConvertsFromStringAndNormalizes(string input, string expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(MnemonicAbbr));
        Assert.True(converter.CanConvertFrom(typeof(string)));
        var abbr = (MnemonicAbbr)converter.ConvertFrom(context: null, CultureInfo.InvariantCulture, input)!;
        Assert.Equal(expected, abbr.Value);
    }

    [Fact]
    public void TypeConverter_ConvertsToNormalizedString()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(MnemonicAbbr));
        Assert.True(converter.CanConvertTo(typeof(string)));
        object? s = converter.ConvertTo(MnemonicAbbr.From("dSe"), typeof(string));
        Assert.Equal("DSE", s);
    }

    [Fact]
    public void ConfigureOpenApiSchema_DeclaresThreeLetterStringConstraint()
    {
        var schema = new OpenApiSchema();
        MnemonicAbbr.ConfigureOpenApiSchema(schema);

        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(expected: 3, schema.MinLength);
        Assert.Equal(expected: 3, schema.MaxLength);
        Assert.Equal("^[A-Z]{3}$", schema.Pattern);
        Assert.Equal(MnemonicAbbr.Pattern, schema.Pattern);
    }
}
