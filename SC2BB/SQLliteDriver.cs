using System;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace SQLiteTools
{
    public class SQLiteDriver
    {
        private string dbFileName;
        private SQLiteConnection connection; 

        public SQLiteDriver(string dbFileName)
        {
            try
            {
                this.dbFileName = dbFileName;
                if (!File.Exists(this.dbFileName) && dbFileName != ":memory:")
                    SQLiteConnection.CreateFile(this.dbFileName);
                
                this.connection = new SQLiteConnection();                              
                this.connection.ConnectionString = String.Format("Data Source={0};Version=3;", dbFileName);
                this.connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public int NonExecuteQuery(string query)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand(this.connection);
                command.CommandText = query;                
                command.CommandType = CommandType.Text;
                return command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }

        public SQLiteDataReader Query(string query)
         {
            SQLiteCommand command = new SQLiteCommand(this.connection);
            command.CommandText = query;            
            command.CommandType = CommandType.Text;            
            SQLiteDataReader reader = command.ExecuteReader();
            return reader;
        }

        public void Close()
        {
            try
            {
                this.connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
