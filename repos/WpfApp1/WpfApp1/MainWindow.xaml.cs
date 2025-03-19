using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly string dbFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfAppUserDB.mdf");
        private readonly string connectionString;

        private ObservableCollection<String> nameList = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();

            connectionString = $@"DATA SOURCE=(LocalDB)\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;";

            NameList.ItemsSource = nameList;
            InitializeDatabase();
            LoadNames();

        }
        private void InitializeDatabase()
        {
            if (!System.IO.File.Exists(dbFilePath)) // Check if DB exists
            {
                using (var connection = new SqlConnection($@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;"))
                {
                    connection.Open();
                    string createDbQuery = $"CREATE DATABASE [WpfAppUserDB] ON (NAME = N'WpfAppUserDB', FILENAME = '{dbFilePath}')";
                    using (var command = new SqlCommand(createDbQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            // Connect to local DB and create table if not exists
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string createTableQuery = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Names' and xtype='U')
                                    CREATE TABLE Names (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(50))";
                using (var command = new SqlCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }


        private void LoadNames()
        {
            nameList.Clear();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "Select Name from Names";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nameList.Add(reader.GetString(0));
                        }
                    }
                }


            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
         String NewName = Namefield.Text.Trim();
            if (!string.IsNullOrEmpty(NewName) && !nameList.Contains(NewName))
            {
                using (var connection = new SqlConnection(connectionString))
                {

                    connection.Open();
                    string query = "INSERT INTO Names (Name) VALUES (@name)";
                        using (var command = new SqlCommand (query, connection))
                    {
                        command.Parameters.AddWithValue("@name", NewName);
                        command.ExecuteNonQuery();
                    }
                }

                nameList.Add(NewName);
                Namefield.Clear();
            }
            

        }

        private void DeleteName_Click(object sender, RoutedEventArgs e)
        {
            if (NameList.SelectedItem is string selectedName)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Names WHERE Name = @name";
                        using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", selectedName);
                        command.ExecuteNonQuery();

                    }
                }
                nameList.Remove(selectedName);
            }
        }

        private string selectedOldName = null; 
        private void UpdateMenu_Click(object sender, RoutedEventArgs e)
        {
            if (NameList.SelectedItem is string selectedName && selectedOldName == null)
            {
                // Step 1: Move selected name to text field (prepares for update)
                Namefield.Text = selectedName;
                selectedOldName = selectedName;
            }
            else if (selectedOldName != null)
            {
                // Step 2: User enters a new name and clicks "Update"
                string newName = Namefield.Text.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != selectedOldName)
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "UPDATE Names SET Name = @newName WHERE Name = @oldName";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@newName", newName);
                            command.Parameters.AddWithValue("@oldName", selectedOldName);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                int index = nameList.IndexOf(selectedOldName);
                                if (index != -1)
                                {
                                    nameList[index] = newName; // Update UI
                                }
                                selectedOldName = null; // Reset selection
                                Namefield.Clear();
                            }
                            else
                            {
                                MessageBox.Show("Update failed! Name not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                    LoadNames(); // Refresh UI from database
                }
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if(NameList.Visibility == Visibility.Visible)
            {
                NameList.Visibility = Visibility.Collapsed;
            }
            else
            {
                NameList.Visibility = Visibility.Visible;
                LoadNames();

            }
            
        }


    }
}

