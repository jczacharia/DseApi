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
                mne.Should().Be(MnemonicAbbr.From("DSE"));
                return TypedResults.Json(new { mne });
            });
        });
        var response = await host.CreateClient()
            .GetFromJsonAsync<Dictionary<string, string>>("/mne/dSe", TestContext.Current.CancellationToken);
        response?["mne"].Should().Be("DSE");
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
        MnemonicAbbr.From(input).Value.Should().Be(expected);

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
        MnemonicAbbr.TryParse(input, provider: null, out _).Should().BeFalse();

    [Fact]
    public void TryParse_NullString_ReturnsFalse() =>
        MnemonicAbbr.TryParse(s: null, provider: null, out _).Should().BeFalse();

    [Fact]
    public void Parse_TrimsAndNormalizes() =>
        MnemonicAbbr.Parse("   dSe ", provider: null).Value.Should().Be("DSE");

    [Fact]
    public void From_InvalidInput_ThrowsFormatException()
    {
        Action act = () => MnemonicAbbr.From("nope");
        act.Should().Throw<FormatException>().WithMessage("*^[A-Z]{3}$*");
    }

    [Fact]
    public void ImplicitStringConversion_YieldsNormalizedString()
    {
        string s = MnemonicAbbr.From("dSe");
        s.Should().Be("DSE");
    }

    [Fact]
    public void ImplicitStringConversion_FlowsToStringParameter()
    {
        Func<string, string> echo = value => value;
        echo(MnemonicAbbr.From("dSe")).Should().Be("DSE");
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByNormalization()
    {
        MnemonicAbbr lower = MnemonicAbbr.From("dse");
        MnemonicAbbr upper = MnemonicAbbr.From("DSE");

        lower.Should().Be(upper);
        (upper == lower).Should().BeTrue();
        lower.GetHashCode().Should().Be(upper.GetHashCode());
    }

    [Fact]
    public void Equality_DistinctValues_AreNotEqual()
    {
        MnemonicAbbr.From("DSF").Should().NotBe(MnemonicAbbr.From("DSE"));
        (MnemonicAbbr.From("DSE") != MnemonicAbbr.From("DSF")).Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey()
    {
        var map = new Dictionary<MnemonicAbbr, int> { [MnemonicAbbr.From("DSE")] = 42 };
        map[MnemonicAbbr.From("dse")].Should().Be(42);
    }

    [Fact]
    public void ISpanParsable_RoundTrips()
    {
        MnemonicAbbr.TryParse("dSe", provider: null, out MnemonicAbbr viaTryParse).Should().BeTrue();
        viaTryParse.Should().Be(MnemonicAbbr.Parse("dSe", provider: null));
        viaTryParse.Value.Should().Be("DSE");
    }

    [Theory]
    [InlineData("dSe", "DSE")]
    [InlineData("DSE", "DSE")]
    public void Json_DeserializesAndNormalizes(string json, string expected)
    {
        var abbr = JsonSerializer.Deserialize<MnemonicAbbr>($"\"{json}\"");
        abbr.Should().Be(MnemonicAbbr.From(expected));
    }

    [Fact]
    public void Json_Serializes_AsNormalizedString() =>
        JsonSerializer.Serialize(MnemonicAbbr.From("dSe")).Should().Be("\"DSE\"");

    [Fact]
    public void Json_RoundTrips()
    {
        MnemonicAbbr original = MnemonicAbbr.From("dSe");
        string json = JsonSerializer.Serialize(original);
        JsonSerializer.Deserialize<MnemonicAbbr>(json).Should().Be(original);
    }

    // MVC binds simple route/query params via TypeDescriptor.GetConverter(...).ConvertFrom(string).
    [Theory]
    [InlineData("dSe", "DSE")]
    [InlineData("   abc ", "ABC")] // trimmed, like every other entry point
    public void TypeConverter_ConvertsFromStringAndNormalizes(string input, string expected)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(MnemonicAbbr));
        converter.CanConvertFrom(typeof(string)).Should().BeTrue();
        var abbr = (MnemonicAbbr)converter.ConvertFrom(context: null, CultureInfo.InvariantCulture, input)!;
        abbr.Value.Should().Be(expected);
    }

    [Fact]
    public void TypeConverter_ConvertsToNormalizedString()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(MnemonicAbbr));
        converter.CanConvertTo(typeof(string)).Should().BeTrue();
        object? s = converter.ConvertTo(MnemonicAbbr.From("dSe"), typeof(string));
        s.Should().Be("DSE");
    }

    [Fact]
    public void ConfigureOpenApiSchema_DeclaresThreeLetterStringConstraint()
    {
        var schema = new OpenApiSchema();
        MnemonicAbbr.ConfigureOpenApiSchema(schema);

        schema.Type.Should().Be(JsonSchemaType.String);
        schema.MinLength.Should().Be(3);
        schema.MaxLength.Should().Be(3);
        schema.Pattern.Should().Be("^[A-Z]{3}$");
        schema.Pattern.Should().Be(MnemonicAbbr.Pattern);
    }
}
