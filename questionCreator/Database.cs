using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;


namespace questionCreator
{
    public class Database
    {
        private static string userName = "guykashtan";
        private static string password = "9bg-h7-c";
        private static string dataSource = "duntgmpjdn.database.windows.net,1433";
        private static string dbName = "triviDb";
        public SqlConnection conn;

        public Database()
        {
            SqlConnectionStringBuilder connString1Builder;
            connString1Builder = new SqlConnectionStringBuilder();
            connString1Builder.DataSource = dataSource;
            connString1Builder.InitialCatalog = dbName;
            connString1Builder.Encrypt = true;
            connString1Builder.TrustServerCertificate = false;
            connString1Builder.UserID = userName;
            connString1Builder.Password = password;

            //conn = new SqlConnection(connString1Builder.ToString());            
            conn = new SqlConnection("Server=tcp:duntgmpjdn.database.windows.net,1433;Database=triviDb;User ID=guykashtan@duntgmpjdn;Password=9bg-h7-c;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;");
            conn.Open();

            //conn.Close();
        }

        public SqlDataReader getAllQuestions()
        {
            SqlCommand command = conn.CreateCommand();
            string cmdText = String.Format("SELECT * FROM Questions");
            command.CommandText = cmdText;
            return command.ExecuteReader();
        }

        public void shutdown()
        {
            conn.Close();
        }

        public void addQuestion(string p1, string answer, string[] wrongAnswers, string link) 
        {
            SqlCommand command = conn.CreateCommand();
            string cmdText = "INSERT INTO QUESTIONS " +
                             "VALUES(@p1, 0.5, 1, 1, @answer, @wanswer1, @wanswer2, @wanswer3, @answer, @link)";
            command.CommandText = cmdText;
            
            command.Parameters.AddWithValue("@p1", p1);
            command.Parameters.AddWithValue("@answer", answer);
            command.Parameters.AddWithValue("@link", link);
            command.Parameters.AddWithValue("@wanswer1", wrongAnswers[0]);
            command.Parameters.AddWithValue("@wanswer2", wrongAnswers[1]);
            command.Parameters.AddWithValue("@wanswer3", wrongAnswers[2]);
            var reader = command.ExecuteReader();
            reader.Close();
        }

        public void addCategory(string catName, string catLink)
        {
            SqlCommand command = conn.CreateCommand();
            string cmdText = String.Format("INSERT INTO Cateogries " +
                            "VALUES('{0}', '{1}')", catName, catLink);
            command.CommandText = cmdText;
            command.ExecuteNonQuery();
        }

        private string escapeStrings(string str)
        {
            if (str == null) return str;

            return str.Replace("'", "\\'");
        }

        public IDataRecord getRandomQuestion()
        {
            SqlCommand command = conn.CreateCommand();
            string cmdText = "SELECT TOP 1 * FROM QUESTIONS ORDER BY NEWID()";
            command.CommandText = cmdText;
            
            var reader = command.ExecuteReader();
            reader.Read();
            return reader;
        }
    }
}
