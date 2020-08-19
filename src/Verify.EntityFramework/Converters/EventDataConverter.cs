using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using VerifyTests;

class EventDataConverter :
    WriteOnlyJsonConverter<CommandEndEventData>
{
    public override void WriteJson(JsonWriter writer, CommandEndEventData? data, JsonSerializer serializer)
    {
        if (data == null)
        {
            return;
        }

        serializer.Serialize(writer, data.ToString());
    }
}