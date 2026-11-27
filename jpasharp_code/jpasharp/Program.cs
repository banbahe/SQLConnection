using System;
using System.Data;
using System.Data.SqlClient;

namespace jpasharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("welcome");

            SQLConection context = new SQLConection(msg => Console.Error.WriteLine(msg));

            // Example 1: Consulta parametrizada segura
            var queryParams = new[]
            {
                new SqlParameter("@id", 1)
            };
            var result = context.ExecuteQuery("SELECT namecolumn FROM table_name WHERE id = @id", queryParams);
            foreach (DataRow item in result.Rows)
            {
                Console.WriteLine(item["namecolumn"]);
                Console.WriteLine(item[0]); // position column
            }

            // Example 2: Comando parametrizado sin retorno de filas
            var updateParams = new[]
            {
                new SqlParameter("@value", "ejemplo"),
                new SqlParameter("@id", 1)
            };
            context.ExecuteNonQuery("UPDATE table_name SET col = @value WHERE id = @id", updateParams);

            // Example 3: Procedimiento almacenado con DBNull.Value explícito
            var valueparameter = "10";
            context.Parametros.Clear();
            context.Parametros.Add(new SqlParameter("@nameparameter", valueparameter));
            context.Parametros.Add(new SqlParameter("@nameparameter_nullable", DBNull.Value));
            var dtResult = context.ExecuteProcedure("sp_name", true);

            if (dtResult.Rows.Count > 0 && dtResult.Rows[0][0] != DBNull.Value)
            {
                var value = dtResult.Rows[0][0].ToString();
                Console.WriteLine(value);
            }

            Console.ReadLine();
        }
    }
}
