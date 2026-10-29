using CoreOne.ODataBuilders;

namespace CoreOne.Converters;

public class ODataOperatorConverter
{
    public class NewtonsoftConverter() : LookupTypeConverter<ODataOperator>.NewtonsoftConverter(ODataOperator.Default)
    {
    }

    public class SystemJsonConverter() : LookupTypeConverter<ODataOperator>.SystemJsonConverter(ODataOperator.Default)
    {
    }
}