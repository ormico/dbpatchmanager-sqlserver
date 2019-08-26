using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ormico.DbPatchManager.Logic;
using Ormico.SqlGoSplitter;

namespace Ormico.DbPatchManager.SqlServer
{
    public class SqlDatabase : IDatabase
    {
        public void Connect(DatabaseOptions options)
        {
            _options = options;
            _con = new SqlConnection(options.ConnectionString);
            _con.Open();
            InitPatchTable();
        }

        DatabaseOptions _options;
        SqlConnection _con;

        Lazy<string> _sqlAddInstalledPatch = new Lazy<string>(() => GetEmbeddedSql("Ormico.DbPatchManager.SqlServer.SqlScripts.AddInstalledPatch.sql"));
        Lazy<string> _sqlInitPatchTable = new Lazy<string>(() => GetEmbeddedSql("Ormico.DbPatchManager.SqlServer.SqlScripts.InitPatchTable.sql"));
        Lazy<string> _sqlGetInstalledPatches = new Lazy<string>(() => GetEmbeddedSql("Ormico.DbPatchManager.SqlServer.SqlScripts.GetInstalledPatches.sql"));
        Lazy<string> _sqlTemp = new Lazy<string>(() => GetEmbeddedSql("Ormico.DbPatchManager.SqlServer.SqlScripts.temp.sql"));

        public void Dispose()
        {
            if(_con != null)
            {
                _con.Dispose();
            }
        }

        public void ExecuteDDL(string commandText)
        {
            var splitter = new ScriptSplitter(commandText);
            IEnumerable<string> enumerable = splitter;
            int i = 0;
            foreach (string sql in enumerable)
            {
                i++;
                //set a timeout of 60 seconds * 60 minutes = 3600 seconds
                //todo: make a global and/or patch specific setting in config
                _con.Execute(sql, commandTimeout: 3600);
            }
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            List<InstalledPatchInfo> rc = _con.Query<InstalledPatchInfo>(_sqlGetInstalledPatches.Value, null).ToList();
            return rc;
        }

        public void LogInstalledPatch(string patchId)
        {
            _con.Execute(_sqlAddInstalledPatch.Value,
                new
                {
                    PatchId = patchId
                }, commandType: System.Data.CommandType.Text);
        }

        void InitPatchTable()
        {
            ExecuteDDL(_sqlInitPatchTable.Value);
        }

        public static string GetEmbeddedSql(string fileName)
        {
            string rc = null;
            Assembly ass = Assembly.GetExecutingAssembly();
            using (StreamReader sr = new StreamReader(ass.GetManifestResourceStream(fileName)))
            {
                rc = sr.ReadToEnd();
            }
        
            return rc;
        }

        public void ExecuteProgrammabilityScript(string commandText)
        {
            ExecuteDDL(commandText);
        }
    }
}
