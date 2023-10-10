using System.Collections.Generic;

namespace CustomShared.Db;

public interface IRepoSaverForFlushSaver<TType, TSchema>
    where TSchema : ISaveSchema
{
    public void Save(
        DocsWithSchema<TType, TSchema> docsWithSchema);

    public string GetName();
}

public interface IDbTableChecker
{
    public bool TableExists(
        string tableName);
}

public interface ISaveSchema
{
}

public class DocsWithSchema<TDocs, TSchema>
    where TSchema : ISaveSchema
{
    public TSchema SaveSchema { get; set; }

    public IEnumerable<TDocs> Docs { get; set; }
}
