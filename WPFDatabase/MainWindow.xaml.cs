using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace WPFDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SqlConnection connection;
        public MainWindow()
        {
            InitializeComponent();
            connection = new SqlConnection(ConfigurationManager.ConnectionStrings["connstr"].ConnectionString);
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            string sql = "SELECT Table_name FROM INFORMATION_SCHEMA.TABLES";
            TreeViewItem northWindChild = new TreeViewItem
            {
                Header = "NorthWind"
            };
            SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
            DataSet ds = new DataSet();
            _ = adapter.Fill(ds, "tables");
            DataView dv = ds.Tables["tables"].AsDataView();
            foreach (DataRowView item in dv)
            {
                TreeViewItem treeChild = new TreeViewItem
                {
                    Header = item["Table_name"],
                };
                TreeViewItem schemaChild = new TreeViewItem
                {
                    Header = "Schema"
                };
                _ = treeChild.Items.Add(schemaChild);
                treeChild.Selected += TreeViewItem_Selected;
                schemaChild.Selected += SchemaItem_Selected;

                _ = northWindChild.Items.Add(treeChild);

            }
            _ = DatabaseTree.Items.Add(northWindChild);
        }
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem t = (TreeViewItem)sender;
            string tableName = t.Header.ToString();
            string sql = string.Format($"Select * from [{tableName}]");
            SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
            DataSet ds = new DataSet();
            _ = adapter.Fill(ds, tableName);
            TableGrid.DataContext = ds.Tables[tableName].AsDataView();
        }
        private void SchemaItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem t = (TreeViewItem)sender;
            TreeViewItem p = (TreeViewItem)t.Parent;

            string tableName = p.Header.ToString();
            string sql = string.Format($"select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{tableName}'");
            SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
            DataSet ds = new DataSet();
            _ = adapter.Fill(ds, tableName);
            TableGrid.DataContext = ds.Tables[tableName].AsDataView();
            e.Handled = true;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sql;
                SqlCommand command;
                SqlDataAdapter adapter;
                DataTable dt = new DataTable();
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                if (TableName.Text.Trim(' ') == "")
                {
                    _ = MessageBox.Show("Enter Table Name");
                    return;
                }
                if (ColumnName1.Text.Trim(' ') == "" && ColumnName2.Text.Trim(' ') == "")
                {
                    _ = MessageBox.Show("Enter Atleast one Column");
                }
                else if (ColumnName1.Text.Trim(' ') != "" && ColumnName2.Text.Trim(' ') != "")
                {
                    sql = $"Select [{ColumnName1.Text}], [{ColumnName2.Text}] from [{TableName.Text}]";
                    command = new SqlCommand(sql, connection);
                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                }
                else if (ColumnName1.Text.Trim(' ') != "")
                {
                    sql = $"Select {ColumnName1.Text} from [{TableName.Text}]";
                    command = new SqlCommand(sql, connection);
                    string query = command.CommandText;
                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                }
                else
                {
                    sql = $"Select {ColumnName2.Text} from [{TableName.Text}]";
                    command = new SqlCommand(sql, connection);
                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                }
                SearchGrid.DataContext = dt.AsDataView();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }

        }

        private void SearchXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tablename = XmlTable.Text;
                SqlCommand command = new SqlCommand("PARSETABLETOXML", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@TA", tablename);
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                DataSet dataSet = new DataSet();

                using (XmlReader xmlReader = command.ExecuteXmlReader())
                {
                    dataSet.ReadXml(xmlReader);
                }
                XmlGrid.ItemsSource = dataSet.Tables[0].DefaultView;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //D:\Code\EMP.xml
            try
            {
                SqlCommand command = new SqlCommand("PARSEXMLTOTABLE", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                FileStream fs = new FileStream(XMLPathText.Text, FileMode.Open);
                SqlXml sqlXml = new SqlXml(fs);
                command.Parameters.Add(new SqlParameter { ParameterName = "@INPUTXML", SqlDbType = SqlDbType.Xml, Value = sqlXml });
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                int output = command.ExecuteNonQuery();
                if (output != -1)
                {
                    MessageBox.Show("sccess");
                }
                else
                {
                    MessageBox.Show("failure");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}
