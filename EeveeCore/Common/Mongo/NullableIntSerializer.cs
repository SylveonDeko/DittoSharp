using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace EeveeCore.Common.Mongo;

/// <summary>
///     Custom MongoDB BSON serializer for nullable integers that handles various input types.
/// </summary>
public class NullableIntSerializer : IBsonSerializer<int?>
{
    /// <summary>
    ///     Gets the type of value that this serializer supports.
    /// </summary>
    public Type ValueType => typeof(int?);

    /// <summary>
    ///     Deserializes a BSON value to a nullable integer.
    /// </summary>
    /// <param name="context">The deserialization context.</param>
    /// <param name="args">The deserialization arguments.</param>
    /// <returns>The deserialized nullable integer value.</returns>
    /// <exception cref="BsonSerializationException">Thrown when the BSON type cannot be converted to a nullable integer.</exception>
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

    /// <summary>
    ///     Serializes a nullable integer to BSON.
    /// </summary>
    /// <param name="context">The serialization context.</param>
    /// <param name="args">The serialization arguments.</param>
    /// <param name="value">The nullable integer value to serialize.</param>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, int? value)
    {
        if (!value.HasValue)
            context.Writer.WriteNull();
        else
            context.Writer.WriteInt32(value.Value);
    }

    /// <summary>
    ///     Deserializes a BSON value to an object.
    /// </summary>
    /// <param name="context">The deserialization context.</param>
    /// <param name="args">The deserialization arguments.</param>
    /// <returns>The deserialized object.</returns>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    /// <summary>
    ///     Serializes an object to BSON.
    /// </summary>
    /// <param name="context">The serialization context.</param>
    /// <param name="args">The serialization arguments.</param>
    /// <param name="value">The object to serialize.</param>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (int?)value);
    }
}