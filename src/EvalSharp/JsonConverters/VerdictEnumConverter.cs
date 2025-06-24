using System.Text.Json;
using System.Text.Json.Serialization;
using EvalSharp.Models.Enums;

namespace EvalSharp.JsonConverters;

internal class VerdictEnumConverter : JsonConverter<VerdictEnum>
{
    public override VerdictEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //Sometimes the LLM will respond with True and False instead of yes or no.
        if (reader.TokenType == JsonTokenType.True)
        {
            return VerdictEnum.Yes;
        }
        if (reader.TokenType == JsonTokenType.False)
        {
            return VerdictEnum.No;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            return VerdictEnum.Idk;
        }

        string enumValue = reader.GetString()!;

        if (Enum.TryParse(enumValue, ignoreCase: true, out VerdictEnum result))
        {
            return result;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, VerdictEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}