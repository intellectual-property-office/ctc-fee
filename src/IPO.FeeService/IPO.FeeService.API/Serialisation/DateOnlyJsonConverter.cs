using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPO.FeeService.API.Serialisation
{
        public class DateOnlyJsonConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => DateTime.Parse(reader.GetString()!).Date;

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }

}
