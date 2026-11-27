using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace jpasharp
{
    #region Conexión a BD

    public class SQLConection
    {
        #region Variables

        private const int DefaultCommandTimeoutSeconds = 30;
        private const string ConnectionStringEnvVar = "JPASHARP_CONNECTION_STRING";
        private const string DefaultConnectionStringName = "DEFAULT";

        private string connectionString;
        private int commandTimeoutSeconds = DefaultCommandTimeoutSeconds;
        private List<SqlParameter> parameters = new List<SqlParameter>();
        private readonly Action<string> logger;

        #endregion

        #region Propiedades

        /// <summary>
        /// Cadena de conexión a la base de datos.
        /// </summary>
        public string ConnectionString
        {
            get { return connectionString; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(value));

                connectionString = value;
            }
        }

        /// <summary>
        /// Tiempo máximo de espera (en segundos) para la ejecución de comandos SQL.
        /// </summary>
        public int CommandTimeout
        {
            get { return commandTimeoutSeconds; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "El tiempo de espera debe ser mayor que cero.");

                commandTimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Lista de parámetros para procedimientos almacenados.
        /// </summary>
        public List<SqlParameter> Parametros
        {
            get { return parameters; }
            set { parameters = value ?? new List<SqlParameter>(); }
        }

        #endregion

        #region Constructores

        /// <summary>
        /// Inicializa una instancia cargando la cadena de conexión desde la variable de entorno
        /// <c>JPASHARP_CONNECTION_STRING</c> o desde <c>App.config</c> (<c>DEFAULT</c>).
        /// </summary>
        /// <param name="logger">Delegado opcional para registrar errores sin exponer secretos.</param>
        public SQLConection(Action<string> logger = null)
            : this(ResolveDefaultConnectionString(), logger)
        {
        }

        /// <summary>
        /// Inicializa una instancia con una cadena de conexión explícita.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión válida.</param>
        /// <param name="logger">Delegado opcional para registrar errores sin exponer secretos.</param>
        public SQLConection(string connectionString, Action<string> logger = null)
        {
            ConnectionString = connectionString;
            this.logger = logger;
        }

        #endregion

        #region Publics

        /// <summary>
        /// Ejecuta una instrucción Transact-SQL sin retorno de filas.
        /// </summary>
        /// <param name="query">Instrucción Transact-SQL a ejecutar.</param>
        /// <returns>Número de filas afectadas.</returns>
        public int ExecuteNonQuery(string query)
        {
            return ExecuteNonQuery(query, null);
        }

        /// <summary>
        /// Ejecuta una instrucción Transact-SQL parametrizada sin retorno de filas.
        /// </summary>
        /// <param name="query">Instrucción Transact-SQL parametrizada.</param>
        /// <param name="sqlParameters">Parámetros de la consulta.</param>
        /// <returns>Número de filas afectadas.</returns>
        public int ExecuteNonQuery(string query, IEnumerable<SqlParameter> sqlParameters)
        {
            ValidateSqlText(query, nameof(query));
            return ExecuteNonQueryInternal(query, CommandType.Text, sqlParameters);
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado de Transact-SQL y devuelve el número de filas afectadas.
        /// </summary>
        /// <param name="storeProcedure">Nombre del procedimiento almacenado.</param>
        /// <param name="withParameters">Indica si deben enviarse los parámetros de <see cref="Parametros"/>.</param>
        /// <returns>Número de filas afectadas.</returns>
        public int ExecuteNonQuery(string storeProcedure, bool withParameters)
        {
            ValidateSqlText(storeProcedure, nameof(storeProcedure));
            return ExecuteNonQueryInternal(storeProcedure, CommandType.StoredProcedure, withParameters ? parameters : null);
        }

        /// <summary>
        /// Ejecuta una consulta SQL y devuelve el resultado en un <see cref="DataTable"/>.
        /// </summary>
        /// <param name="query">Consulta SQL a ejecutar.</param>
        /// <returns><see cref="DataTable"/> con los resultados.</returns>
        public DataTable ExecuteQuery(string query)
        {
            return ExecuteQuery(query, null);
        }

        /// <summary>
        /// Ejecuta una consulta SQL parametrizada y devuelve el resultado en un <see cref="DataTable"/>.
        /// </summary>
        /// <param name="query">Consulta SQL parametrizada.</param>
        /// <param name="sqlParameters">Parámetros de la consulta.</param>
        /// <returns><see cref="DataTable"/> con los resultados.</returns>
        public DataTable ExecuteQuery(string query, IEnumerable<SqlParameter> sqlParameters)
        {
            ValidateSqlText(query, nameof(query));

            try
            {
                using (var conn = CreateOpenConnection())
                using (var cmd = CreateCommand(conn, query, CommandType.Text, sqlParameters))
                {
                    try
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var dt = new DataTable();
                            dt.Load(reader);
                            return dt;
                        }
                    }
                    finally
                    {
                        cmd.Parameters.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(nameof(ExecuteQuery), ex);
                throw;
            }
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado de consulta y devuelve la primera tabla en un <see cref="DataTable"/>.
        /// </summary>
        /// <param name="storeProcedure">Nombre del procedimiento almacenado.</param>
        /// <param name="withParameters">Indica si deben enviarse los parámetros de <see cref="Parametros"/>.</param>
        /// <returns><see cref="DataTable"/> con los resultados.</returns>
        public DataTable ExecuteProcedure(string storeProcedure, bool withParameters)
        {
            DataSet ds = ExecuteStoredProcedure(storeProcedure, withParameters);
            return ds.Tables.Count != 0 ? ds.Tables[0] : new DataTable();
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado de consulta y devuelve el resultado en un <see cref="DataSet"/>.
        /// </summary>
        /// <param name="storeProcedure">Nombre del procedimiento almacenado.</param>
        /// <param name="withParameters">Indica si deben enviarse los parámetros de <see cref="Parametros"/>.</param>
        /// <returns><see cref="DataSet"/> con los resultados.</returns>
        public DataSet ExecuteStoredProcedure(string storeProcedure, bool withParameters)
        {
            return ExecuteStoredProcedure(storeProcedure, withParameters ? parameters : null);
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado parametrizado y devuelve el resultado en un <see cref="DataSet"/>.
        /// </summary>
        /// <param name="storeProcedure">Nombre del procedimiento almacenado.</param>
        /// <param name="sqlParameters">Parámetros del procedimiento almacenado.</param>
        /// <returns><see cref="DataSet"/> con los resultados.</returns>
        public DataSet ExecuteStoredProcedure(string storeProcedure, IEnumerable<SqlParameter> sqlParameters)
        {
            ValidateSqlText(storeProcedure, nameof(storeProcedure));

            try
            {
                using (var conn = CreateOpenConnection())
                using (var cmd = CreateCommand(conn, storeProcedure, CommandType.StoredProcedure, sqlParameters))
                {
                    try
                    {
                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            var ds = new DataSet();
                            adapter.Fill(ds);
                            return ds;
                        }
                    }
                    finally
                    {
                        cmd.Parameters.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(nameof(ExecuteStoredProcedure), ex);
                throw;
            }
        }

        #endregion

        #region Helpers

        private static string ResolveDefaultConnectionString()
        {
            string envConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(envConnectionString))
                return envConnectionString;

            ConnectionStringSettings configEntry = ConfigurationManager.ConnectionStrings[DefaultConnectionStringName];
            if (configEntry == null || string.IsNullOrWhiteSpace(configEntry.ConnectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró una cadena de conexión válida. Configure 'JPASHARP_CONNECTION_STRING' o la entrada 'DEFAULT' en App.config.");
            }

            return configEntry.ConnectionString;
        }

        private static void ValidateSqlText(string sqlText, string paramName)
        {
            if (string.IsNullOrWhiteSpace(sqlText))
                throw new ArgumentException("La instrucción SQL o procedimiento almacenado no puede ser nulo ni vacío.", paramName);
        }

        private SqlConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        internal SqlCommand CreateCommand(SqlConnection conn, string sql, CommandType commandType, IEnumerable<SqlParameter> sqlParameters)
        {
            var cmd = new SqlCommand(sql, conn)
            {
                CommandType = commandType,
                CommandTimeout = commandTimeoutSeconds
            };

            if (sqlParameters != null)
            {
                foreach (SqlParameter parameter in sqlParameters)
                {
                    if (parameter == null)
                        continue;

                    if (parameter.Value == null)
                        parameter.Value = DBNull.Value;

                    cmd.Parameters.Add(parameter);
                }
            }

            return cmd;
        }

        private int ExecuteNonQueryInternal(string sql, CommandType commandType, IEnumerable<SqlParameter> sqlParameters)
        {
            try
            {
                using (var conn = CreateOpenConnection())
                using (var cmd = CreateCommand(conn, sql, commandType, sqlParameters))
                {
                    try
                    {
                        return cmd.ExecuteNonQuery();
                    }
                    finally
                    {
                        cmd.Parameters.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(nameof(ExecuteNonQuery), ex);
                throw;
            }
        }

        internal void LogError(string operation, Exception ex)
        {
            if (logger == null)
                return;

            var sqlEx = ex as SqlException;
            if (sqlEx != null)
            {
                logger(string.Format("[SQLConection.{0}] SqlException #{1}: {2}", operation, sqlEx.Number, sqlEx.Message));
                return;
            }

            logger(string.Format("[SQLConection.{0}] {1}: {2}", operation, ex.GetType().Name, ex.Message));
        }

        #endregion
    }

    #endregion
}
