using System.Collections.Generic;

namespace CustomShared.Db;

public interface IDbInterface<TType, TSchema, TDocsWithSchema>
    where TSchema : ISaveSchema
    where TDocsWithSchema : DocsWithSchema<TType, TSchema>
{
    public void Save(TDocsWithSchema docsWithSchema);

    public string GetName();
}

public interface IDbTableChecker
{
    public bool TableExists(string tableName);
}

public interface ISaveSchema
{
}

public class DocsWithSchema<TDocs, TSchema> where TSchema : ISaveSchema
{
    public TSchema SaveSchema { get; set; }
    public IEnumerable<TDocs> Docs { get; set; }
}