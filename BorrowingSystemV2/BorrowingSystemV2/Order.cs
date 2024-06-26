﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowingSystemV2
{
    public partial class Order : Form
    {
        public string mySqlServerName = "sql6.freemysqlhosting.net";
        public string mySqlServerUserId = "sql6696982";
        public string mySqlServerPassword = "DJyeU2QQMU";
        public string mySqlDatabaseName = "sql6696982";

        public Order()
        {
            InitializeComponent();
        }

        public bool CheckTextboxes()
        {
            foreach (Control control in Controls)
            {
                if (control is TextBox)
                {
                    if (string.IsNullOrEmpty(((TextBox)control).Text))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        private void Order_Load(object sender, EventArgs e)
        {
            try
            {
                availableLabel.Text = "";
                quantityLabel.Text = "";
                MySqlConnection connection = new MySqlConnection($"datasource={mySqlServerName};port=3306;username={mySqlServerUserId};password={mySqlServerPassword};database={mySqlDatabaseName}");
                connection.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT equipmentName, quantity FROM sql6696982.inventory WHERE quantity>0", connection);

                MySqlDataReader reader = cmd.ExecuteReader();

                while(reader.Read())
                {
                    string eqName = reader.GetString("equipmentName");
                    string display = $"{eqName}";
                    equipmentName.Items.Add(display);
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }  
           
        }

        private void submitOrderBTN_Click(object sender, EventArgs e)
        {
            if (studentIDTxtbx.Text == "" || subjectCodeTxtbx.Text == "" || instructorName.Text == "" || equipmentName.Text == "" || quantityTxtbx.Text == "")
            {
                MessageBox.Show("Please provide all necessary information", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if(!int.TryParse(quantityTxtbx.Text, out int quantity))
            {
                MessageBox.Show("The quantity must be a whole number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MySqlConnection connection = new MySqlConnection($"datasource={mySqlServerName};port=3306;username={mySqlServerUserId};password={mySqlServerPassword};database={mySqlDatabaseName}");
            connection.Open();

            //Check if an Equipment Existed in the Database and store the Equipment ID
            MySqlCommand checkStudentCommand = connection.CreateCommand();
            checkStudentCommand.CommandType = CommandType.Text;
            checkStudentCommand.CommandText = "SELECT studentId FROM sql6696982.students WHERE studentId = @student_id";
            checkStudentCommand.Parameters.AddWithValue("@student_id", studentIDTxtbx.Text);
            MySqlDataReader reader = checkStudentCommand.ExecuteReader();

            if (!reader.HasRows)
            {
                MessageBox.Show("The ID of the student you provided does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                connection.Close();
                return;
            }

            reader.Read();
            reader.Close();

          //  connection.Open();
            //Check if an Equipment Existed in the Database and store the Equipment ID
            MySqlCommand checkEquipmentCommand1 = connection.CreateCommand();
            checkEquipmentCommand1.CommandType = CommandType.Text;
            checkEquipmentCommand1.CommandText = "SELECT equipmentID FROM sql6696982.inventory WHERE equipmentName = @equipmentName";
            checkEquipmentCommand1.Parameters.AddWithValue("@equipmentName", equipmentName.Text);
            MySqlDataReader reader1 = checkEquipmentCommand1.ExecuteReader();

            if (!reader1.HasRows)
            {
                MessageBox.Show("Equipment does not exist in inventory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                connection.Close();
                return;
            }

            reader1.Read();
            int equipmentId = reader1.GetInt32("equipmentID");
            reader1.Close();

            // Check the current quantity in the inventory
            MySqlCommand checkQuantityCommand = connection.CreateCommand();
            checkQuantityCommand.CommandType = CommandType.Text;
            checkQuantityCommand.CommandText = "SELECT quantity FROM sql6696982.inventory WHERE equipmentID = @equipment_id";
            checkQuantityCommand.Parameters.AddWithValue("@equipment_id", equipmentId);
            var currentQuantity = Convert.ToInt32(checkQuantityCommand.ExecuteScalar());

           
            //If the current quantity is less than the quantity the user wants to borrow, show a MessageBox and stop
            if (currentQuantity < Convert.ToInt32(quantityTxtbx.Text))
            {
                MessageBox.Show($"There are only {currentQuantity} left in the inventory", "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to submit this order?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Update the Equipment ID in the inventory by subtracting the quantity
                    MySqlCommand checkEquipmentCommand2 = connection.CreateCommand();
                    checkEquipmentCommand2.CommandType = CommandType.Text;
                    checkEquipmentCommand2.CommandText = "UPDATE sql6696982.inventory SET quantity = quantity - @quantity WHERE equipmentID = @equipment_id";
                    checkEquipmentCommand2.Parameters.AddWithValue("@quantity", quantityTxtbx.Text);
                    checkEquipmentCommand2.Parameters.AddWithValue("@equipment_id", equipmentId);
                    checkEquipmentCommand2.ExecuteNonQuery();
                    //Insert Order in the Database
                    DateTime order_DATE = DateTime.Now;
                    DateTime order_TIME = DateTime.Now;
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT INTO sql6696982.orders (subject_code, instructor_name, quantity, student_ID, staff_ID, admin_ID, order_DATE, order_TIME, equipment_id) VALUES (@subject_code, @instructor_name, @quantity, @student_ID, @staff_ID, @admin_id, @order_DATE, @order_TIME, @equipment_id)";
                    command.Parameters.AddWithValue("@subject_code", subjectCodeTxtbx.Text);
                    command.Parameters.AddWithValue("@instructor_name", instructorName.Text);
                    command.Parameters.AddWithValue("@student_ID", studentIDTxtbx.Text);
                    command.Parameters.AddWithValue("@quantity", quantityTxtbx.Text);
                    command.Parameters.AddWithValue("@staff_ID", StaffLogin.EmployeeID);
                    command.Parameters.AddWithValue("@admin_ID", AdminLogin.EmployeeID);
                    command.Parameters.AddWithValue("@order_DATE", order_DATE.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@order_TIME", order_TIME.ToString("hh:mm:ss:tt"));
                    command.Parameters.AddWithValue("@equipment_id", equipmentId);
                    command.ExecuteNonQuery();

                    connection.Close();
                    studentIDTxtbx.Text = "";
                    subjectCodeTxtbx.Text = "";
                    instructorName.Text = "";
                    equipmentName.Text = "";
                    quantityTxtbx.Text = "";
                    instructorName.SelectedIndex = -1;
                    equipmentName.SelectedIndex = -1;

                    MessageBox.Show("Submitted Successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    quantityLabel.Text = "";
                    availableLabel.Text = "";
                }
            }

        }


        private void studentIDTxtbx_TextChanged(object sender, EventArgs e)
        {
            if (studentIDTxtbx.Text.Length == 11)
            {
                if (studentIDTxtbx.Text != "")
                {
                    MySqlConnection connection = null;
                    MySqlDataReader reader = null;

                    try
                    {
                        connection = new MySqlConnection("datasource=" + mySqlServerName + ";port=3306;username=" + mySqlServerUserId + ";password=" + mySqlServerPassword + ";database=" + mySqlDatabaseName + ";");
                        connection.Open();

                        MySqlCommand cmd = new MySqlCommand("SELECT studentName, course, yearLevel FROM sql6696982.students where studentId = @studentID", connection);
                        cmd.Parameters.AddWithValue("@studentID", (studentIDTxtbx.Text));

                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            studentNameTxtbx.Text = reader["studentName"].ToString();
                            courseTxtbx.Text = reader["course"].ToString();
                            yearTxtbx.Text = reader["yearLevel"].ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        if (connection != null)
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                studentNameTxtbx.Text = "";
                courseTxtbx.Text = "";
                yearTxtbx.Text = "";
            }

        }

        private void clearOrderBTN_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to clear all fields?", "Clear Fields", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    instructorName.Text = "";
                    equipmentName.Text = "";
                    quantityLabel.Text = "";
                    availableLabel.Text = "";
                    foreach (Control c in clearpanel.Controls)
                    {
                        if (c is TextBox)
                        {
                            ((TextBox)c).Clear();
                        }
                        else if (c is ComboBox)
                        {
                            ((ComboBox)c).SelectedIndex = -1; // Clear the selected item in the ComboBox
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void equipmentName_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Selecting the equipment name will display the quantity of the equipment through the availableLabel label
            MySqlConnection connection = new MySqlConnection($"datasource={mySqlServerName};port=3306;username = {mySqlServerUserId};password={mySqlServerPassword};database={mySqlDatabaseName}");
            connection.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT quantity FROM sql6696982.inventory WHERE equipmentName = @equipmentName", connection);
            cmd.Parameters.AddWithValue("@equipmentName", equipmentName.Text);
            MySqlDataReader reader = cmd.ExecuteReader();
            if(reader.Read())
            {
                quantityLabel.Text = "The available quantity of the equipment is:";
                availableLabel.Text = $"{reader.GetInt16("quantity")}";

            }
        }
    }
}
