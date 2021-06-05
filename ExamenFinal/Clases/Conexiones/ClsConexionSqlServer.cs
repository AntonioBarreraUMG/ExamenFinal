using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamenFinal.Clases.Conexiones
{
    class ClsConexionSqlServer
    {
        private readonly SqlConnection conexion;
        private readonly String cadenaConexion;

        public ClsConexionSqlServer()
        {
            cadenaConexion = "Data Source=DESKTOP-6QVFLKL\\SQLEXPRESS;Initial Catalog=dbExamenFinal;Integrated Security=True";
            conexion = new SqlConnection(cadenaConexion);
        }

        private void abrirConexion()
        {
            conexion.Open();
        }

        private void cerrarConexion()
        {
            conexion.Close();
        }

        public DataTable consultarDB(String sql)
        {
            abrirConexion();
            SqlDataReader dr;
            SqlCommand com = new SqlCommand(sql, conexion);
            dr = com.ExecuteReader();
            var dt = new DataTable();
            dt.Load(dr);
            cerrarConexion();
            return dt;
        }

        public void ejecutarSql(String sql)
        {
            abrirConexion();
            try
            {
                SqlCommand comm = new SqlCommand(sql, conexion);
                comm.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
            finally
            {
                cerrarConexion();
            }
        }
    }
}
