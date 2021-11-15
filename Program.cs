namespace SqlServerSchemaExportTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var exportScripts = new ExportScripts();
            exportScripts.CreateScriptDataBase();
        }
    }
}