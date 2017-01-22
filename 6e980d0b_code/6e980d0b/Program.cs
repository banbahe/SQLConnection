using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data;
using System.Data.SqlClient;

namespace _6e980d0b
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("welcome");

            SQLConection context = new SQLConection();

            // Example 1
            var result = context.ExecuteQuery("Select query as result");
            foreach (DataRow item in result.Rows)
            {
                Console.WriteLine(item["namecolumn"]);
                Console.WriteLine(item[0]); // position column 
            }

            // Example 2
            context.ExecuteNonQuery("Select query as result");

            // Example 3
            var valueparameter = "10";
            context.Parametros.Clear();
            context.Parametros.Add(new SqlParameter("@nameparameter", valueparameter));
            context.Parametros.Add(new SqlParameter("@nameparameter...", null));
            var dtResult = context.ExecuteProcedure("sp_name", true);

            var value = dtResult.Rows[0][0].ToString();
            Console.WriteLine(value.ToString());

            Console.ReadLine();
        }

        
    }
}
