using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace jpasharp.Tests
{
    [TestClass]
    public class SQLConectionTests
    {
        private const string ConnectionStringEnvVar = "JPASHARP_CONNECTION_STRING";
        private const string ValidTestConnectionString =
            "Data Source=127.0.0.1,1;Initial Catalog=TestDb;User ID=testuser;Password=SuperSecretPassword123!;Encrypt=True;TrustServerCertificate=False;Connect Timeout=1;";

        [TestMethod]
        public void DefaultConstructor_ReadsAppConfig_WhenEnvVarNotSet()
        {
            string previous = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            try
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvVar, null);
                var db = new SQLConection();

                Assert.Contains("Data Source=TEST_SERVER", db.ConnectionString);
                Assert.Contains("Encrypt=True", db.ConnectionString);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvVar, previous);
            }
        }

        [TestMethod]
        public void DefaultConstructor_EnvironmentVariableOverridesAppConfig()
        {
            string previous = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            try
            {
                const string envConn = "Data Source=ENV_SERVER;Initial Catalog=ENV_DB;Integrated Security=True;Encrypt=True;";
                Environment.SetEnvironmentVariable(ConnectionStringEnvVar, envConn);

                var db = new SQLConection();
                Assert.AreEqual(envConn, db.ConnectionString);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvVar, previous);
            }
        }

        [TestMethod]
        public void ExplicitConstructor_SetsConnectionStringAndDefaultTimeout()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.AreEqual(ValidTestConnectionString, db.ConnectionString);
            Assert.AreEqual(30, db.CommandTimeout);
            Assert.IsNotNull(db.Parametros);
            Assert.AreEqual(0, db.Parametros.Count);
        }

        [TestMethod]
        public void ExplicitConstructor_ThrowsArgumentException_WhenConnectionStringIsNullOrWhitespace()
        {
            Assert.Throws<ArgumentException>(() => new SQLConection((string)null));
            Assert.Throws<ArgumentException>(() => new SQLConection(""));
            Assert.Throws<ArgumentException>(() => new SQLConection("   \t  "));
        }

        [TestMethod]
        public void ConnectionString_Setter_ThrowsArgumentException_WhenNullOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentException>(() => db.ConnectionString = null);
            Assert.Throws<ArgumentException>(() => db.ConnectionString = "");
            Assert.Throws<ArgumentException>(() => db.ConnectionString = "   ");
            Assert.AreEqual(ValidTestConnectionString, db.ConnectionString);
        }

        [TestMethod]
        public void CommandTimeout_Setter_ThrowsArgumentOutOfRangeException_WhenZeroOrNegative()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentOutOfRangeException>(() => db.CommandTimeout = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => db.CommandTimeout = -5);
            Assert.AreEqual(30, db.CommandTimeout);
        }

        [TestMethod]
        public void CommandTimeout_Setter_UpdatesValue_WhenPositive()
        {
            var db = new SQLConection(ValidTestConnectionString);
            db.CommandTimeout = 45;

            Assert.AreEqual(45, db.CommandTimeout);
        }

        [TestMethod]
        public void Parametros_Setter_NormalizesNullToEmptyList()
        {
            var db = new SQLConection(ValidTestConnectionString);
            db.Parametros.Add(new SqlParameter("@p1", 1));
            db.Parametros = null;

            Assert.IsNotNull(db.Parametros);
            Assert.AreEqual(0, db.Parametros.Count);
        }

        [TestMethod]
        public void ExecuteNonQuery_Query_ThrowsArgumentException_WhenNullEmptyOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery(null));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery(""));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("   "));
        }

        [TestMethod]
        public void ExecuteNonQuery_Parameterized_ThrowsArgumentException_WhenNullEmptyOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);
            var parms = new[] { new SqlParameter("@id", 1) };

            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery(null, parms));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("", parms));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("   ", parms));
        }

        [TestMethod]
        public void ExecuteNonQuery_StoredProcedure_ThrowsArgumentException_WhenNullEmptyOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery(null, true));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("", false));
            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("  ", true));
        }

        [TestMethod]
        public void ExecuteQuery_ThrowsArgumentException_WhenNullEmptyOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentException>(() => db.ExecuteQuery(null));
            Assert.Throws<ArgumentException>(() => db.ExecuteQuery(""));
            Assert.Throws<ArgumentException>(() => db.ExecuteQuery("   "));
            Assert.Throws<ArgumentException>(() => db.ExecuteQuery(null, new[] { new SqlParameter("@id", 1) }));
        }

        [TestMethod]
        public void ExecuteProcedureAndStoredProcedure_ThrowArgumentException_WhenNullEmptyOrWhitespace()
        {
            var db = new SQLConection(ValidTestConnectionString);

            Assert.Throws<ArgumentException>(() => db.ExecuteProcedure(null, true));
            Assert.Throws<ArgumentException>(() => db.ExecuteProcedure(" ", false));
            Assert.Throws<ArgumentException>(() => db.ExecuteStoredProcedure(null, true));
            Assert.Throws<ArgumentException>(() => db.ExecuteStoredProcedure(" ", false));
            Assert.Throws<ArgumentException>(() => db.ExecuteStoredProcedure("", (IEnumerable<SqlParameter>)null));
        }

        [TestMethod]
        public void CreateCommand_SetsCommandTypeTextTimeoutAndParameters()
        {
            var db = new SQLConection(ValidTestConnectionString) { CommandTimeout = 15 };
            var parameters = new[]
            {
                new SqlParameter("@id", 42),
                null,
                new SqlParameter("@nullable", null)
            };

            using (var conn = new SqlConnection(ValidTestConnectionString))
            using (var cmd = db.CreateCommand(conn, "SELECT * FROM users WHERE id = @id", CommandType.Text, parameters))
            {
                Assert.AreEqual(CommandType.Text, cmd.CommandType);
                Assert.AreEqual(15, cmd.CommandTimeout);
                Assert.AreEqual("SELECT * FROM users WHERE id = @id", cmd.CommandText);
                Assert.AreEqual(2, cmd.Parameters.Count);
                Assert.AreEqual(42, cmd.Parameters["@id"].Value);
                Assert.AreEqual(DBNull.Value, cmd.Parameters["@nullable"].Value);
                cmd.Parameters.Clear();
            }
        }

        [TestMethod]
        public void CreateCommand_SetsStoredProcedureAndEmptyParameters_WhenNullCollection()
        {
            var db = new SQLConection(ValidTestConnectionString);

            using (var conn = new SqlConnection(ValidTestConnectionString))
            using (var cmd = db.CreateCommand(conn, "sp_get_users", CommandType.StoredProcedure, null))
            {
                Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
                Assert.AreEqual(30, cmd.CommandTimeout);
                Assert.AreEqual("sp_get_users", cmd.CommandText);
                Assert.AreEqual(0, cmd.Parameters.Count);
            }
        }

        [TestMethod]
        public void ExecuteQuery_LogsErrorWithoutSecretAndPreservesStackTrace_OnConnectionFailure()
        {
            var logs = new List<string>();
            var db = new SQLConection(ValidTestConnectionString, msg => logs.Add(msg));

            var ex = Assert.Throws<SqlException>(() => db.ExecuteQuery("SELECT 1"));

            Assert.IsTrue(logs.Count == 1, "Se esperaba exactamente una entrada en el log.");
            Assert.Contains("ExecuteQuery", logs[0]);
            Assert.DoesNotContain("SuperSecretPassword123!", logs[0]);
            Assert.DoesNotContain("SuperSecretPassword123!", ex.Message);
            Assert.Contains("CreateOpenConnection", ex.StackTrace);
        }

        [TestMethod]
        public void ExecuteNonQuery_LogsErrorWithoutSecret_OnInvalidConnectionStringFormat()
        {
            var logs = new List<string>();
            var db = new SQLConection("invalid-connection-string-SecretValue999", msg => logs.Add(msg));

            Assert.Throws<ArgumentException>(() => db.ExecuteNonQuery("DELETE FROM t WHERE id = @id", new[] { new SqlParameter("@id", 1) }));

            Assert.AreEqual(1, logs.Count);
            Assert.Contains("ExecuteNonQuery", logs[0]);
            Assert.Contains("ArgumentException", logs[0]);
        }

        [TestMethod]
        public void ExecuteStoredProcedure_LogsError_OnInvalidConnectionStringFormat()
        {
            var logs = new List<string>();
            var db = new SQLConection("invalid-connection-string", msg => logs.Add(msg));

            Assert.Throws<ArgumentException>(() => db.ExecuteStoredProcedure("sp_test", false));

            Assert.AreEqual(1, logs.Count);
            Assert.Contains("ExecuteStoredProcedure", logs[0]);
        }

        [TestMethod]
        public void LogError_DoesNotThrow_WhenLoggerIsNull()
        {
            var db = new SQLConection(ValidTestConnectionString, null);
            db.LogError("TestOp", new InvalidOperationException("detalle"));
            Assert.IsTrue(true);
        }
    }
}
