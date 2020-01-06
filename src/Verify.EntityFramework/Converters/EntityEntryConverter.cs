using Newtonsoft.Json;
using Verify;
using Microsoft.EntityFrameworkCore.ChangeTracking;

class EntityEntryConverter :
    WriteOnlyJsonConverter<EntityEntry>
{
    public override void WriteJson(JsonWriter writer, EntityEntry? context, JsonSerializer serializer)
    {
        if (context == null)
        {
            return;
        }

        writer.WriteStartObject();

        //HttpResponseConverter.WriteProperties(writer, serializer, response);

        writer.WriteEndObject();
    }

}