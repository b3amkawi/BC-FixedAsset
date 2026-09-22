using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BC.FixedAsset.Data
{
    public static class Db
    {
        public static SqlConnection OpenConnection()
        {
            var item = ConfigurationManager.ConnectionStrings["BCFixedAsset"];
            if (item == null || string.IsNullOrWhiteSpace(item.ConnectionString))
                throw new ConfigurationErrorsException("Connection string 'BCFixedAsset' is missing.");
            var connection = new SqlConnection(item.ConnectionString);
            connection.Open();
            return connection;
        }

        public static SqlParameter Parameter(string name, object value, SqlDbType type, int size = 0)
        {
            var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
            if (size > 0) parameter.Size = size;
            return parameter;
        }
    }
}
