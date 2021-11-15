using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace SqlServerSchemaExportTool
{
public class ObjectType
    {
        public const string Table = "Table";
        public const string StoredProcedure = "StoredProcedure";
        public const string Schema = "Schema";
        public const string View = "View";
        public const string Function = "UserDefinedFunction";
    }

    class Program
    {
        public static Server GetConnectionDatabase()
        {
            var server = new Server();

            server.ConnectionContext.LoginSecure = false;
            server.ConnectionContext.Login = "sa";
            server.ConnectionContext.Password = "yourStrong(!)Password";
            server.ConnectionContext.ServerInstance = "hades.ubuntu,1433";

            return server;
        }

        public static List<string> GetDatabases(Server server)
        {
            var databaseList = new List<string>();

            var databases = server.Databases;

            foreach (Database database in databases)
            {
                if (database.IsSystemObject == false)
                {
                    databaseList.Add(database.Name);
                }
            }

            return databaseList;
        }

        public static Scripter GetScripter(Server server)
        {
            var scripter = new Scripter(server);

            scripter.Options.AllowSystemObjects = false;
            scripter.Options.AnsiFile = true;
            scripter.Options.AppendToFile = true;
            scripter.Options.Bindings = true;
            scripter.Options.ContinueScriptingOnError = true;
            scripter.Options.DriAllConstraints = true;
            scripter.Options.EnforceScriptingOptions = true;
            scripter.Options.ExtendedProperties = true;
            scripter.Options.FullTextIndexes = true;
            scripter.Options.IncludeDatabaseContext = true;
            scripter.Options.IncludeHeaders = false; // Include descriptive headers
            scripter.Options.IncludeIfNotExists = false;
            scripter.Options.Indexes = false; // Script indexs
            scripter.Options.NoCollation = true;
            scripter.Options.NoCommandTerminator = true;
            scripter.Options.Permissions = true;
            scripter.Options.SchemaQualify = true;
            scripter.Options.ScriptBatchTerminator = true;
            scripter.Options.ScriptData = false; // Types of data to script
            scripter.Options.ScriptDrops = false; // Script DROP
            scripter.Options.ScriptSchema = true; // Types of data to script
            scripter.Options.ToFileOnly = true;
            scripter.Options.Triggers = true; // Script triggers
            scripter.Options.WithDependencies = false;
            scripter.PrefetchObjects = true;

            return scripter;
        }

        public static void GenerateFile(StringBuilder sb, string folder, string schema, string name, string type)
        {
            const string extension = "sql";

            if (!String.IsNullOrWhiteSpace(schema))
                schema = $"{schema}.";

            var fileName = $"{folder}{schema}{name}.{type}.{extension}";

            using (var sw = File.CreateText(fileName))
            {
                sw.Write(sb);
            }
        }

        public static void GenerateScript(StringBuilder sb, string script)
        {
            if (String.IsNullOrWhiteSpace(script))
                return;

            sb.AppendLine(script.Trim());
            sb.AppendLine("GO");
        }

        public static void GetTableScript(Server server, Database database, string folderDatabase)
        {
            foreach (Table table in database.Tables)
            {
                if (table.IsSystemObject == true)
                    continue;

                var sb = new StringBuilder();

                var scripts = GetScripter(server).EnumScript(new Urn[] { table.Urn });

                foreach (var script in scripts)
                {
                    GenerateScript(sb, script);
                }

                GenerateFile(sb, folderDatabase, table.Schema, table.Name, ObjectType.Table);
            }
        }

        public static void GetStoredProcedureScript(Server server, Database database, string folderDatabase)
        {
            foreach (StoredProcedure storedProcedure in database.StoredProcedures)
            {
                if (storedProcedure.IsSystemObject == true)
                    continue;

                var sb = new StringBuilder();

                var scripts = GetScripter(server).EnumScript(new Urn[] { storedProcedure.Urn });

                foreach (var script in scripts)
                {
                    GenerateScript(sb, script);
                }

                GenerateFile(sb,
                    folderDatabase, 
                    storedProcedure.Schema,
                    storedProcedure.Name,
                    ObjectType.StoredProcedure);
            }
        }

        public static void GetFunctionScript(Server server, Database database, string folderDatabase)
        {
            foreach (UserDefinedFunction function in database.UserDefinedFunctions)
            {
                if (function.IsSystemObject == true)
                    continue;

                var sb = new StringBuilder();

                var scripts = GetScripter(server).EnumScript(new Urn[] { function.Urn });

                foreach (var script in scripts)
                {
                    GenerateScript(sb, script);
                }

                GenerateFile(sb,
                    folderDatabase, 
                    function.Schema,
                    function.Name,
                    ObjectType.Function);
            }
        }

        public static void GetViewScript(Server server, Database database, string folderDatabase)
        {
            foreach (View view in database.Views)
            {
                if (view.IsSystemObject == true)
                    continue;

                var sb = new StringBuilder();

                var scripts = GetScripter(server).EnumScript(new Urn[] { view.Urn });

                foreach (var script in scripts)
                {
                    GenerateScript(sb, script);
                }

                GenerateFile(sb,
                    folderDatabase, 
                    view.Schema,
                    view.Name,
                    ObjectType.View);
            }
        }

        public static void GetSchemaScript(Server server, Database database, string folderDatabase)
        {
            foreach (Schema schema in database.Schemas)
            {
                if (schema.IsSystemObject == true)
                    continue;

                var sb = new StringBuilder();

                var scripts = GetScripter(server).EnumScript(new Urn[] { schema.Urn });

                foreach (var script in scripts)
                {
                    GenerateScript(sb, script);
                }

                GenerateFile(sb,
                    folderDatabase, 
                    "",
                    schema.Name,
                    ObjectType.Schema);
            }
        }

        public static void CreateScriptDataBase()
        {
            string folderRoot = "Scripts/";

            if (Directory.Exists(folderRoot))
            {
                Directory.Delete(folderRoot, true);
            }

            Directory.CreateDirectory(folderRoot);

            var server = GetConnectionDatabase();

            foreach (var database in GetDatabases(server))
            {
                var databaseSrv = server.Databases["" + database + ""];

                if (databaseSrv != null)
                {
                    string folderDatabase = $"{folderRoot}{database}/";

                    if (!Directory.Exists(folderDatabase))
                    {
                        Directory.CreateDirectory(folderDatabase);
                    }

                    GetTableScript(server, databaseSrv, folderDatabase);
                    GetStoredProcedureScript(server, databaseSrv, folderDatabase);
                    GetFunctionScript(server, databaseSrv, folderDatabase);
                    GetViewScript(server, databaseSrv, folderDatabase);
                    GetSchemaScript(server, databaseSrv, folderDatabase);
                }
            }
        }

        static void Main(string[] args)
        {
            CreateScriptDataBase();
        }
    }
}