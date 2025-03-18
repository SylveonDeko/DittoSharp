using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Ditto.Common.Mongo;

public class NullableIntSerializer : IBsonSerializer<int?>
{
    public Type ValueType => typeof(int?);

    public int? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        switch (type)
        {
            case BsonType.Int32:
                return context.Reader.ReadInt32();
            case BsonType.String:
                var str = context.Reader.ReadString();
                if (string.IsNullOrEmpty(str))
                    return null;
                if (int.TryParse(str, out var result))
                    return result;
                return null;
            case BsonType.Null:
                context.Reader.ReadNull();
                return null;
            default:
                throw new BsonSerializationException($"Cannot deserialize value of type {type} to nullable int");
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, int? value)
    {
        if (!value.HasValue)
            context.Writer.WriteNull();
        else
            context.Writer.WriteInt32(value.Value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (int?)value);
    }
}