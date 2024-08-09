#region Using directives
using FTOptix.DataLogger;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using System;
using System.Linq;
using UAManagedCore;
using FTOptix.Store;
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class EmbeddedDatabaseSpaceCalculator : BaseNetLogic
{
    [ExportMethod]
    public void CalculateSQLiteSpace()
    {
        // Insert code to be executed by the method
        NodeId loggerNodeId = LogicObject.GetVariable("DataLoggerObject").Value;
        if (loggerNodeId == null)
        {
            Log.Error("CalculateSQLiteSpace", "DataLoggerObject does not contain a valid NodeId");
            return;
        }
        var loggerObject = InformationModel.Get<DataLogger>(loggerNodeId);
        if (loggerObject == null)
        {
            Log.Error("CalculateSQLiteSpace", "DataLoggerObject does not exist in the current project");
            return;
        }
        var loggerStore = InformationModel.Get(loggerObject.Store);
        if (!(loggerStore is SQLiteStore))
        {
            Log.Warning("CalculateSQLiteSpace", $"The store of DataLogger '{loggerObject}' is not an instance of SQLiteStore.");
            return;
        }
        // https://www.sqlite.org/datatype3.html
        // Each value stored in an SQLite database (or manipulated by the database engine) has one of the following storage classes:
        // NULL. The value is a NULL value.
        // INTEGER. The value is a signed integer, stored in 0, 1, 2, 3, 4, 6, or 8 bytes depending on the magnitude of the value.
        // REAL. The value is a floating point value, stored as an 8-byte IEEE floating point number.
        // TEXT. The value is a text string, stored using the database encoding (UTF-8, UTF-16BE or UTF-16LE).
        // BLOB. The value is a blob of data, stored exactly as it was input.
        // A storage class is more general than a datatype. The INTEGER storage class, for example, includes 7 different integer datatypes of different lengths. This makes a difference on disk. But as soon as INTEGER values are read off of disk and into memory for processing, they are converted to the most general datatype (8-byte signed integer). And so for the most part, "storage class" is indistinguishable from "datatype" and the two terms can be used interchangeably.
        // Any column in an SQLite version 3 database, except an INTEGER PRIMARY KEY column, may be used to store a value of any storage class.
        // All values in SQL statements, whether they are literals embedded in SQL statement text or parameters bound to precompiled SQL statements have an implicit storage class. Under circumstances described below, the database engine may convert values between numeric storage classes (INTEGER and REAL) and TEXT during query execution.
        var variablesToLog = loggerObject.VariablesToLog.OfType<VariableToLog>();
        Log.Debug("CalculateSQLiteSpace", $"Found '{variablesToLog.Count()}' Variables to log");
        UInt32 sizePerLog = 0;
        foreach (VariableToLog loggedVar in variablesToLog)
        {
            var dataType = GetDataTypeIfBaseDataType(loggedVar);

            if (dataType == OpcUa.DataTypes.Int16 ||
                dataType == OpcUa.DataTypes.Int32 ||
                dataType == OpcUa.DataTypes.Int64 ||
                dataType == OpcUa.DataTypes.Float ||
                dataType == OpcUa.DataTypes.Double ||
                dataType == OpcUa.DataTypes.Boolean ||
                dataType == OpcUa.DataTypes.Byte ||
                dataType == OpcUa.DataTypes.SByte ||
                dataType == OpcUa.DataTypes.UInt16 ||
                dataType == OpcUa.DataTypes.UInt32)
            {
                sizePerLog += 8;
            }
            else if (dataType == OpcUa.DataTypes.DateTime ||
                     dataType == OpcUa.DataTypes.UtcTime)
            {
                sizePerLog += 27;
            }
            else
            {
                Log.Warning("CalculateSQLiteSpace.Unknown", $"Cannot calculate space for '{loggedVar.BrowseName}', unsupported data type");
            }
        }
        // Add the Timestamp and LocalTimestamp
        sizePerLog += 27;
        if (loggerObject.GetVariable("LogLocalTime").Value)
        {
            sizePerLog += 27;
        }
        // Adding the ID column
        sizePerLog += 8;
        // Calculate sampling time and number of samples per hour
        var samplingPeriod = loggerObject.SamplingPeriod;
        var sizePerMin = 600000 / samplingPeriod;
        // Print output
        Log.Info("CalculateSQLiteSpace.SpacePerLog", $"Logger is consuming '{sizePerLog}' Bytes ({sizePerLog / 1024} KB) every {samplingPeriod} ms");
        Log.Info("CalculateSQLiteSpace.SpacePer5min", $"Logger will consume '{sizePerLog * sizePerMin}' Bytes ({sizePerLog * sizePerMin / 1024} KB) per minute");
        Log.Info("CalculateSQLiteSpace.SpacePer1hour", $"Logger will consume '{sizePerLog * sizePerMin * 60}' Bytes ({sizePerLog * sizePerMin * 60 / 1024} KB) per hour");
    }

    private NodeId GetDataTypeIfBaseDataType(VariableToLog inputVariable)
    {
        // In some cases, the data type of the variable to log is BaseDataType
        // and the actual data type has to be retrieved from the dynamic link
        if (inputVariable.DataType == OpcUa.DataTypes.BaseDataType)
        {
            var dynamicLink = inputVariable.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink);

            if (dynamicLink == null)
            {
                Log.Warning("CalculateSQLiteSpace.GetDataTypeIfBaseDataType", $"Cannot find any dynamic link for '{Log.Node(inputVariable)}', cannot retrieve original data type");
                return NodeId.Empty;
            }

            var dynamicLinkPath = (string)dynamicLink.Value;
            var targetVariable = LogicObject.Context.ResolvePath(inputVariable, dynamicLinkPath).ResolvedNode;

            if (targetVariable == null)
            {
                Log.Warning("CalculateSQLiteSpace.GetDataTypeIfBaseDataType", $"Cannot resolve source variable for '{Log.Node(inputVariable)}'");
                return NodeId.Empty;
            }

            Log.Debug("CalculateSQLiteSpace.GetDataTypeIfBaseDataType", $"Resolved path '{dynamicLinkPath}' to '{targetVariable.BrowseName}' ({targetVariable.NodeId})");
            try
            {
                return ((IUAVariable)targetVariable).DataType;
            }
            catch (Exception ex)
            {
                Log.Error("CalculateSQLiteSpace.GetDataTypeIfBaseDataType", $"Error while getting data type of {Log.Node(targetVariable)}: {ex.Message}");
                return NodeId.Empty;
            }
        }
        else
        {
            return inputVariable.DataType;
        }
    }
}
