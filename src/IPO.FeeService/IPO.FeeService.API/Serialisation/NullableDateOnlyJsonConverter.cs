using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPO.FeeService.API.Serialisation
{
    public class NullableDateOnlyJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return string.IsNullOrWhiteSpace(str) ? null : DateTime.Parse(str).Date;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
            else
                writer.WriteNullValue();
        }
    }
}
