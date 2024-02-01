using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OneCore.Converters;

namespace OneCore.Models;

public class NewtonSettings : JsonSerializerSettings
{
    public static readonly NewtonSettings Default = new();

    public NewtonSettings()
    {
        Formatting = Formatting.None;
        NullValueHandling = NullValueHandling.Ignore;
        DateParseHandling = DateParseHandling.DateTime;
        DateFormatHandling = DateFormatHandling.IsoDateFormat;
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        ContractResolver = new DefaultContractResolver {
            NamingStrategy = new CamelCaseNamingStrategy()
        };
        Converters = [new StringEnumConverter(), new FileSizeConverter.NewtonsoftConverter()];
    }

    public NewtonSettings AddConverter(JsonConverter? converter)
    {
        if (converter is not null)
            Converters.Add(converter);
        return this;
    }
}