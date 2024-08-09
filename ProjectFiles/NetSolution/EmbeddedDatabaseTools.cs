#region Using directives
using System;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using UAManagedCore;
using FTOptix.Store;
#endregion

public class EmbeddedDatabaseTools : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    /// <summary>
    /// Get the relative path of the embedded database
    /// </summary>
    /// <param name="embeddedDatabaseNodeId">The NodeId of the embedded database</param>
    /// <param name="path">The relative path of the embedded database</param>
    [ExportMethod]
    public static void GetRelativeDatabasePath(NodeId embeddedDatabaseNodeId, out string path)
    {
        try
        {
            var embeddedDatabase = GetStoreFromModel(embeddedDatabaseNodeId);
            path = DatabasePath(embeddedDatabase);
        }
        catch (Exception exception)
        {
            Log.Error("EmbeddedDatabaseTools.GetRelativeDatabasePath", $"Failed to get the relative path of the embedded database, exception: {exception}");
            path = string.Empty;
        }
    }

    /// <summary>
    /// Get the absolute path of the embedded database
    /// </summary>
    /// <param name="embeddedDatabaseNodeId">The NodeId of the embedded database</param>
    /// <param name="path">The absolute path of the embedded database</param>
    [ExportMethod]
    public static void GetAbsoluteDatabasePath(NodeId embeddedDatabaseNodeId, out string path)
    {
        try
        {
            var embeddedDatabase = GetStoreFromModel(embeddedDatabaseNodeId);
            path = DatabasePath(embeddedDatabase).Uri;
        }
        catch (Exception exception)
        {
            Log.Error("EmbeddedDatabaseTools.GetAbsoluteDatabasePath", $"Failed to get the absolute path of the embedded database, exception: {exception}");
            path = string.Empty;
        }
    }

    /// <summary>
    /// Get the size of the embedded database
    /// Please note that the size is calculated by reading the file size, it may not be accurate if the database is in use
    /// </summary>
    /// <param name="embeddedDatabaseNodeId">The NodeId of the embedded database</param>
    /// <param name="sizeBytes">The size in bytes of the database</param>
    /// <param name="sizeKiloBytes">The size in kilobytes of the database</param>
    /// <param name="sizeMegaBytes">The size in megabytes of the database</param>
    [ExportMethod]
    public static void GetDatabaseSize(NodeId embeddedDatabaseNodeId, out long sizeBytes, out long sizeKiloBytes, out long sizeMegaBytes)
    {
        try
        {
            var embeddedDatabase = GetStoreFromModel(embeddedDatabaseNodeId);
            sizeBytes = new System.IO.FileInfo(DatabasePath(embeddedDatabase).Uri).Length;
            // Change the division to 1024 in order to get kibibytes and mibibytes
            sizeKiloBytes = sizeBytes / 1000;
            sizeMegaBytes = sizeKiloBytes / 1000;
        }
        catch (Exception exception)
        {
            Log.Error("EmbeddedDatabaseTools.GetDatabaseSize", $"Failed to get the size of the embedded database, exception: {exception}");
            sizeBytes = -1;
            sizeKiloBytes = -1;
            sizeMegaBytes = -1;
        }
    }

    /// <summary>
    /// Check if the embedded database is in use by the FTOptix application
    /// </summary>
    /// <param name="embeddedDatabaseNodeId">The NodeId of the embedded database</param>
    /// <param name="inUse">True if the database is in use, false otherwise</param>
    [ExportMethod]
    public static void IsDatabaseInUse(NodeId embeddedDatabaseNodeId, out bool inUse)
    {
        try
        {
            var embeddedDatabase = GetStoreFromModel(embeddedDatabaseNodeId);
            inUse = System.IO.File.Exists(DatabasePath(embeddedDatabase).Uri + "-wal");
        }
        catch (Exception exception)
        {
            Log.Error("EmbeddedDatabaseTools.IsDatabaseInUse", $"Failed to check if the embedded database is in use, exception: {exception}");
            inUse = false;
        }
    }

    /// <summary>
    /// Get the store object from the project model
    /// </summary>
    /// <param name="embeddedDatabaseNodeId">The NodeId of the embedded database</param>
    /// <exception cref="ArgumentException">
    /// An ArgumentException is thrown if the store could not be loaded from model or if
    /// the database is stored in memory (no sqlite file on disk)
    /// </exception>
    private static SQLiteStore GetStoreFromModel(NodeId embeddedDatabaseNodeId)
    {
        var embeddedDatabase = InformationModel.Get<SQLiteStore>(embeddedDatabaseNodeId) ??
            throw new ArgumentException("SQLiteStore not found, make sure the target node is an embedded database");
        if (embeddedDatabase.InMemory)
            throw new ArgumentException("SQLiteStore is in memory, the database file is not stored on disk");
        return embeddedDatabase;
    }

    /// <summary>
    /// Get the path of the embedded database
    /// Dy design, if no filename is specified, the NodeId is used as the filename
    /// </summary>
    /// <param name="embeddedDatabase">The embedded database</param>
    /// <returns>The path of the embedded database as ResourceUri</returns>
    private static ResourceUri DatabasePath(SQLiteStore embeddedDatabase)
    {
        string fileName = embeddedDatabase.Filename;
        if (string.IsNullOrEmpty(fileName))
            fileName = embeddedDatabase.NodeId.Id.ToString().Replace("-", "");
        fileName += ".sqlite";
        return ResourceUri.FromApplicationRelativePath(fileName);
    }
}
