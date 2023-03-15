using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CustomShared.Db;

public class MongoSaveSchema : ISaveSchema
{
    public static readonly MongoSaveSchema Instance = new();
}

public static class MongoCommon
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MongoCommon));

    private static MongoClient _mongoClientProd;
    private static MongoClient _mongoClientTest;

    public static IEnumerable<BsonDocument> ToBsonDocuments<TSource>(
        this IEnumerable<TSource> source)
    {
        return source.Select(x => x.ToBsonDocument()).ToList();
    }

    public static IMongoCollection<BsonDocument> BuildCollection(
        string dbName,
        string collectionName,
        bool isTest = false)
    {
        var mongoClient = GetMongoClient(isTest);

        Log.Info($"Building Mongo collection for [db: {dbName}] [collection: {collectionName}].");

        IMongoDatabase db = mongoClient.GetDatabase(dbName);
        return db.GetCollection<BsonDocument>(collectionName);
    }

    public static MongoClient GetMongoClient(
        MongoConfiguration mongoConfiguration,
        bool isTest = false)
    {
        return GetMongoClient(
            mongoConfiguration.ConnectionString,
            isTest);
    }

    public static MongoClient GetMongoClient(
        string connectionStr,
        bool isTest = false)
    {
        if (_mongoClientProd == null)
        {
            Log.Info($"Mongo production connection string: {connectionStr}");
            _mongoClientProd = new MongoClient(connectionStr);
        }

        if (_mongoClientTest == null)
        {
            Log.Info($"Mongo test connection string: {connectionStr}");
            _mongoClientTest = new MongoClient(connectionStr);
        }

        return isTest
            ? _mongoClientTest
            : _mongoClientProd;
    }

    public static MongoClient GetMongoClient(
        bool isTest = false)
    {
        return GetMongoClient(
            isTest
                ? ConfigVariables.TestInstance.MongoConn
                : ConfigVariables.Instance.MongoConn,
            isTest);
    }

    public static void SaveOrUpdateSingle<T>(
        this IMongoCollection<T> mongoCollection,
        T record,
        Func<T, ObjectId?> objectIdFromEntryFunc,
        Action<ObjectId> setIdFunc)
    {
        var id = objectIdFromEntryFunc(record);

        if (id is null)
        {
            setIdFunc(ObjectId.GenerateNewId());
            mongoCollection.InsertOne(record);
        }
        else
        {
            mongoCollection.ReplaceOne(
                new BsonDocument(
                    "_id",
                    id),
                record,
                new ReplaceOptions { IsUpsert = true });
        }
    }

    public static void SaveOrUpdateList<T>(
        this IMongoCollection<T> mongoCollection,
        List<T> entries,
        Func<T, ObjectId?> ObjectIdFromEntryFunc)
    {
        var bulkOps = new List<WriteModel<T>>();
        foreach (var record in entries)
        {
            WriteModel<T> writeModel;

            if (ObjectIdFromEntryFunc(record) == null)
            {
                writeModel = new InsertOneModel<T>(record);
            }
            else
            {
                writeModel = new ReplaceOneModel<T>(
                        Builders<T>.Filter.Where(x => ObjectIdFromEntryFunc(x) == ObjectIdFromEntryFunc(record)),
                        record)
                    { IsUpsert = true };
            }

            bulkOps.Add(writeModel);
        }

        mongoCollection.BulkWrite(bulkOps);
    }

    public static void SaveOrUpdateListFromBsonDocuments<T>(
        this IMongoCollection<T> mongoCollection,
        List<T> entries)
        where T : BsonDocument
    {
        var bulkOps = new List<WriteModel<T>>();
        foreach (var record in entries)
        {
            WriteModel<T> writeModel;

            var id = GetId(record);

            if (id == null)
            {
                writeModel = new InsertOneModel<T>(record);
            }
            else
            {
                writeModel = new ReplaceOneModel<T>(
                        Builders<T>.Filter.Where(
                            x => x["_id"] == id),
                        record)
                    { IsUpsert = true };
            }

            bulkOps.Add(writeModel);
        }

        mongoCollection.BulkWrite(bulkOps);
    }

    private static ObjectId? GetId<T>(
        T doc)
        where T : BsonDocument
    {
        try
        {
            return doc["_id"].AsObjectId;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public class BsonDocNoDiscrimatorWriter
    {
        public BsonDocNoDiscrimatorWriter(
            Dictionary<string, object> extras)
        {
            Extras = extras;
        }

        [BsonExtraElements] public Dictionary<string, object> Extras { get; set; }
    }

    public static BsonDocNoDiscrimatorWriter AsBsonDocNoDiscriminatorWriter(
        this Dictionary<string, object> obj) => new(
        obj);
}

public class MongoInterface : IDbInterface<BsonDocument, MongoSaveSchema, MongoDocsWithSchema>
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MongoInterface));

    private readonly IMongoCollection<BsonDocument> _mongoCollection;

    public MongoInterface(
        IMongoCollection<BsonDocument> mongoCollection)
    {
        _mongoCollection = mongoCollection;
    }

    public void Save(
        MongoDocsWithSchema docsWithSchema)
    {
        InsertManyOptions insertManyOptions = new()
        {
            IsOrdered = false
        };
        try
        {
            _mongoCollection.InsertMany(
                docsWithSchema.Docs,
                insertManyOptions);
        }
        catch (MongoBulkWriteException ex)
        {
            var duplicateKeyWriteErrorCount =
                ex.WriteErrors.Count(x => x.Category == ServerErrorCategory.DuplicateKey);
            var allErrorsWereDuplicateKeyErrors = duplicateKeyWriteErrorCount == ex.WriteErrors.Count;
            if (allErrorsWereDuplicateKeyErrors)
            {
                Log.Info($"[{ex.WriteErrors.Count}] Duplicate keys already in DB.");
            }
            else
            {
                var nonDuplicateKeyWriteErrors = ex.WriteErrors.Count - duplicateKeyWriteErrorCount;
                Log.Error($"Error writing [{nonDuplicateKeyWriteErrors}] to db");
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                "Caught unknown error writing to Mongo:",
                ex);
        }
    }

    public string GetName() => "Mongo";
}

public class MongoDocsWithSchema : DocsWithSchema<BsonDocument, MongoSaveSchema>
{
}
