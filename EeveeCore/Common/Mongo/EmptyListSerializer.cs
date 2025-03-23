using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EeveeCore.Common.Mongo;

public class EmptyListSerializer<T>(IBsonSerializer<T>? itemSerializer = null) : SerializerBase<List<T>>
{
    private readonly IBsonSerializer<T> _itemSerializer = itemSerializer ?? BsonSerializer.LookupSerializer<T>();

    public override List<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();

        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return new List<T>();
        }

        if (type != BsonType.Array)
        {
            throw new BsonSerializationException($"Expected array, got {type}");
        }

        context.Reader.ReadStartArray();
        var list = new List<T>();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var item = _itemSerializer.Deserialize(context);
            list.Add(item);
        }

        context.Reader.ReadEndArray();
        return list;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, List<T>? value)
    {
        if (value == null)
        {
            context.Writer.WriteStartArray();
            context.Writer.WriteEndArray();
            return;
        }

        context.Writer.WriteStartArray();
        foreach (var item in value)
        {
            _itemSerializer.Serialize(context, item);
        }
        context.Writer.WriteEndArray();
    }
}