using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace InputMaster
{
  [JsonConverter(typeof(TitleFilterJsonConverter))]
  class TitleFilter
  {
    private Regex Regex;

    public TitleFilter(string value)
    {
      Value = value;
      Regex = Helper.GetRegex(Value);
    }

    public string Value { get; }

    public bool IsEnabled()
    {
      return Regex.IsMatch(Env.ForegroundListener.ForegroundWindowTitle);
    }
  }

  class TitleFilterJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(TitleFilter);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      return new TitleFilter(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.WriteValue(((TitleFilter)value).Value);
    }
  }
}
