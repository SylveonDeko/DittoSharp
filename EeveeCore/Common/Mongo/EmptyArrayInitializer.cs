using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Collections.Generic;
using MongoDB.Bson.IO;

namespace EeveeCore.Common.Mongo
{
    public class NullableListSerializer<T> : IBsonSerializer<List<T>>
    {
        private readonly IBsonSerializer<T> _itemSerializer = BsonSerializer.LookupSerializer<T>();

        public Type ValueType => typeof(List<T>);

        public List<T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();

            if (bsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return new List<T>();
            }

            if (bsonType == BsonType.Array)
            {
                var list = new List<T>();
                context.Reader.ReadStartArray();

                while (context.Reader.State != BsonReaderState.EndOfArray)
                {
                    list.Add(_itemSerializer.Deserialize(context, args));
                }

                context.Reader.ReadEndArray();
                return list;
            }

            throw new BsonSerializationException($"Cannot deserialize value of type {bsonType} to List<{typeof(T).Name}>");
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, List<T> value)
        {
            if (value == null)
            {
                context.Writer.WriteStartArray();
                context.Writer.WriteEndArray();
            }
            else
            {
                context.Writer.WriteStartArray();

                foreach (var item in value)
                {
                    _itemSerializer.Serialize(context, args, item);
                }

                context.Writer.WriteEndArray();
            }
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, (List<T>)value);
        }
    }
}