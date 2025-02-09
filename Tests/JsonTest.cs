using FluentAssertions;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tests;

public class JsonTest {

    [Fact]
    public void dontEscapePlusWhenSerializingStrings() {
        const string DESERIALIZED = "1+2=3";

        JavaScriptEncoder encoder;
        // encoder = JavaScriptEncoder.Default;
        encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        // encoder = JavaScriptEncoder.Create(settings: new TextEncoderSettings(UnicodeRanges.All));
        JsonSerializerOptions options = new(JsonSerializerDefaults.General) {
            Encoder = encoder
        };

        string serialized = JsonSerializer.Serialize(DESERIALIZED, options);

        serialized.Should().Be("\"1+2=3\"");
    }

}