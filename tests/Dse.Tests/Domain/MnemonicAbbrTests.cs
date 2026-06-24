// Copyright (c) PNC Financial Services. All rights reserved.


using System.Net.Http.Json;
using System.Text.Json;
using Dse.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
                Assert.Equal(mne, MnemonicAbbr.Parse("DSE"));
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
    [InlineData("xyz", "XYZ")] // end of the alphabet folds correctly
    public void TryParse_ValidInput_NormalizesToUppercase(string input, string expected)
    {
        Assert.True(MnemonicAbbr.TryParse(input.AsSpan(), out MnemonicAbbr abbr));
        Assert.Equal(expected, abbr.ToString());
    }

    [Theory]
    [InlineData("")] // empty
    [InlineData("D")] // too short
    [InlineData("DS")] // too short
    [InlineData("DSEE")] // too long
    [InlineData("DS1")] // digit
    [InlineData("DS ")] // whitespace
    [InlineData("DS-")] // punctuation
    [InlineData("D[E")] // '[' sits just past 'Z' in ASCII — must not fold into a letter
    [InlineData("D{E")] // '{' folds to '[' under the &~0x20 trick — must still be rejected
    [InlineData("D0E")] // digit that could underflow the range check
    [InlineData("ÀBC")] // non-ASCII letter
    [InlineData("DÉE")] // accented char in the middle
    public void TryParse_InvalidInput_ReturnsFalseAndDefault(string input)
    {
        Assert.False(MnemonicAbbr.TryParse(input.AsSpan(), out MnemonicAbbr abbr));
        Assert.Equal(expected: default, abbr);
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        Assert.False(MnemonicAbbr.TryParse(s: null, provider: null, out MnemonicAbbr abbr));
        Assert.Equal(expected: default, abbr);
    }

    [Fact]
    public void Parse_ValidInput_ReturnsNormalized() =>
        Assert.Equal("DSE", MnemonicAbbr.Parse("dSe").ToString());

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() => MnemonicAbbr.Parse("nope"));
        Assert.Contains("^[A-Z]{3}$", ex.Message);
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByNormalization()
    {
        MnemonicAbbr lower = MnemonicAbbr.Parse("dse");
        MnemonicAbbr upper = MnemonicAbbr.Parse("DSE");

        Assert.Equal(upper, lower);
        Assert.True(upper == lower);
        Assert.Equal(upper.GetHashCode(), lower.GetHashCode());
    }

    [Fact]
    public void Equality_DistinctValues_AreNotEqual()
    {
        Assert.NotEqual(MnemonicAbbr.Parse("DSE"), MnemonicAbbr.Parse("DSF"));
        Assert.True(MnemonicAbbr.Parse("DSE") != MnemonicAbbr.Parse("DSF"));
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey()
    {
        var map = new Dictionary<MnemonicAbbr, int> { [MnemonicAbbr.Parse("DSE")] = 42 };
        Assert.Equal(expected: 42, map[MnemonicAbbr.Parse("dse")]);
    }

    [Fact]
    public void ISpanParsable_StringOverloads_RoundTrip()
    {
        Assert.True(MnemonicAbbr.TryParse("dSe", provider: null, out MnemonicAbbr viaTryParse));
        Assert.Equal(MnemonicAbbr.Parse("dSe", provider: null), viaTryParse);
        Assert.Equal("DSE", viaTryParse.ToString());
    }

    [Theory]
    [InlineData("dSe", "DSE")]
    [InlineData("DSE", "DSE")]
    public void Json_DeserializesAndNormalizes(string json, string expected)
    {
        var abbr = JsonSerializer.Deserialize<MnemonicAbbr>($"\"{json}\"");
        Assert.Equal(MnemonicAbbr.Parse(expected), abbr);
    }

    [Fact]
    public void Json_Serializes_AsNormalizedString() =>
        Assert.Equal("\"DSE\"", JsonSerializer.Serialize(MnemonicAbbr.Parse("dSe")));

    [Fact]
    public void Json_RoundTrips()
    {
        MnemonicAbbr original = MnemonicAbbr.Parse("dSe");
        string json = JsonSerializer.Serialize(original);
        Assert.Equal(original, JsonSerializer.Deserialize<MnemonicAbbr>(json));
    }

    [Fact]
    public void Json_InvalidString_ThrowsFormatException() =>
        Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<MnemonicAbbr>("\"nope\""));
}
