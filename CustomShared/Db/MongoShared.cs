using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace CustomShared.Db;

public static class MongoShared
{
    public static void Setup()
    {
        NodaTimeSerializers.Register();

        // serialize in Mongo enum as string
        var pack = new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String)
        };

        ConventionRegistry.Register(
            "EnumStringConvention",
            pack,
            t => true);
        //

        // default serialization of decimal is string; set it to serialize to BSON decimal128 type
        BsonSerializer.RegisterSerializer(
            typeof(decimal),
            new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(
            typeof(decimal?),
            new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
    }

    public class NullDiscriminatorConvention : IDiscriminatorConvention
    {
        public static NullDiscriminatorConvention Instance { get; }
            = new NullDiscriminatorConvention();

        public Type GetActualType(
            IBsonReader bsonReader,
            Type nominalType)
            => nominalType;

        public BsonValue GetDiscriminator(
            Type nominalType,
            Type actualType)
            => null;

        public string ElementName { get; } = null;
    }

    public class MyDictionarySerializer : SerializerBase<Dictionary<string, object>>
    {
        public override void Serialize(
            BsonSerializationContext ctx,
            BsonSerializationArgs args,
            Dictionary<string, object> dictionary)
        {
            ctx.Writer.WriteStartDocument();

            if (dictionary != null)
            {
                foreach (var kvPair in dictionary)
                {
                    ctx.Writer.WriteName(kvPair.Key);

                    var value = kvPair.Value;

                    if (value is Dictionary<string, object> asDict)
                    {
                        Serialize(
                            ctx,
                            args,
                            asDict);
                    }
                    else
                    {
                        BsonSerializer.Serialize(
                            ctx.Writer,
                            value?.GetType() ?? typeof(object),
                            value);
                    }
                }
            }

            ctx.Writer.WriteEndDocument();
        }

        public override Dictionary<string, object> Deserialize(
            BsonDeserializationContext ctx,
            BsonDeserializationArgs args)
        {
            Dictionary<string, object> dict = new();

            if (ctx.Reader.CurrentBsonType is not BsonType.Document)
                throw new Exception();

            var doc = BsonSerializer.Deserialize<BsonDocument>(ctx.Reader);

            foreach (var bsonElement in doc)
            {
                var name = bsonElement.Name;

                switch (bsonElement.Value.BsonType)
                {
                    case BsonType.Document:
                        var dictInner = new Dictionary<string, object>();
                        MongoShared.DeserializeInner(
                            (BsonDocument)bsonElement.Value,
                            dictInner);
                        dict[name] = dictInner;
                        break;

                    case BsonType.Array:
                        throw new NotImplementedException();
                        break;
                    default:
                    {
                        throw new BsonSerializationException("Unable to deserialize dictionary!");
                    }
                }
            }

            return dict;
        }
    }

    public static void DeserializeInner(
        BsonDocument bsonDocument,
        Dictionary<string, object> dict)
    {
        foreach (var bsonElement in bsonDocument)
        {
            object value;

            if (bsonElement.Value.BsonType is BsonType.Document)
            {
                Dictionary<string, object> innerDict = new();
                value = innerDict;
                DeserializeInner(
                    (BsonDocument)bsonElement.Value,
                    innerDict);
            }
            else
                value = bsonElement.Value;

            dict[bsonElement.Name] = value;
        }
    }

    public static void RemoveTypeDiscriminatorsFromBsonDocumentInPlace(
        this BsonDocument bsonDocument,
        string discriminatorKey = "_t")
    {
        foreach (var bsonElement in bsonDocument.Values)
        {
            if (bsonElement is BsonDocument doc)
            {
                var success = doc.TryGetElement(
                    discriminatorKey,
                    out var elem);

                if (success)
                {
                    doc.RemoveElement(elem);
                    continue;
                }

                RemoveTypeDiscriminatorsFromBsonDocumentInPlace(
                    bsonElement.ToBsonDocument(),
                    discriminatorKey);
            }
        }
    }
}
