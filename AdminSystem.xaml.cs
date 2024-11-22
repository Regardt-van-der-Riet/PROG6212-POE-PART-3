using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;

namespace CMCS
{
    public partial class AdminSystem : Page
    {
        private string connectionString = "Data Source=DataFile3.db;Version=3;";
        private int? selectedClaimId = null;
        private string currentFileInfo = null;

        public AdminSystem()
        {
            InitializeComponent();
            LoadCurrentClaims();
            EnsureStatusColumn();
            DownloadButton.IsEnabled = false;
        }

        private void EnsureStatusColumn()
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = "PRAGMA table_info(ClaimsRecord)";
                        bool statusExists = false;
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetString(1).Equals("Status", StringComparison.OrdinalIgnoreCase))
                                {
                                    statusExists = true;
                                    break;
                                }
                            }
                        }

                        if (!statusExists)
                        {
                            command.CommandText = "ALTER TABLE ClaimsRecord ADD COLUMN Status TEXT";
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCurrentClaims()
        {
            ClaimsListBox.Items.Clear();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ClaimId, LecturerName, LecturerSurname, Status, OvertimeHoursWorked FROM ClaimsRecord";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int claimId = reader.GetInt32(0);
                            string lecturerName = reader.GetString(1);
                            string lecturerSurname = reader.GetString(2);
                            string status = reader.IsDBNull(3) ? "Pending" : reader.GetString(3);
                            int overtimeHoursWorked = reader.GetInt32(4);
                            string displayString = $"ID: {claimId}, Name: {lecturerName} {lecturerSurname}, Status: {status}, Overtime Hours: {overtimeHoursWorked}";
                            ClaimsListBox.Items.Add(displayString);
                        }
                    }
                }
            }
        }

        private void LoadStatusHistory()
        {
            if (!selectedClaimId.HasValue) return;

            StatusHistoryListBox.Items.Clear();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Status, OvertimeHoursWorked, OvertimeHourlyRate FROM ClaimsRecord WHERE ClaimId = @ClaimId";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimId", selectedClaimId.Value);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Get the status of the claim
                            object statusObj = reader["Status"];
                            string status = statusObj != DBNull.Value ? statusObj.ToString() : "Pending";
                            StatusHistoryListBox.Items.Add($"Current Status: {status}");

                            // Get the overtime hours worked and hourly rate
                            int overtimeHoursWorked = reader.GetInt32(reader.GetOrdinal("OvertimeHoursWorked"));
                            decimal overtimeHourlyRate = reader.GetDecimal(reader.GetOrdinal("OvertimeHourlyRate"));

                            // Calculate and display the overtime payment
                            decimal overtimePayment = overtimeHoursWorked * overtimeHourlyRate;
                            StatusHistoryListBox.Items.Add($"Overtime Payment: {overtimePayment:C}");
                        }
                        else
                        {
                            StatusHistoryListBox.Items.Add("No additional status available.");
                        }
                    }
                }
            }
        }


        private void ClaimsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClaimsListBox.SelectedItem != null)
                {
                    string selectedItem = ClaimsListBox.SelectedItem.ToString();
                    int.TryParse(selectedItem.Substring(4, selectedItem.IndexOf(',') - 4), out int selectedClaimIdValue);
                    selectedClaimId = selectedClaimIdValue;
                    ShowClaimDetails(selectedClaimId.Value);
                    LoadStatusHistory();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowClaimDetails(int claimId)
        {
            ClaimsRecord claimDetails = GetClaimDetailsById(claimId);

            if (claimDetails == null)
            {
                MessageBox.Show("No details found for the selected claim.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calculate the claim money amount
            decimal claimMoneyAmount = claimDetails.OvertimeHoursWorked * claimDetails.OvertimeHourlyRate;

            // Construct the details message
            string detailsMessage = $"Claim ID: {claimDetails.ClaimId}\n" +
                                    $"Lecturer Name: {claimDetails.LecturerName} {claimDetails.LecturerSurname}\n" +
                                    $"Hourly Rate: {claimDetails.HourlyRate}\n" +
                                    $"Overtime Hours Worked: {claimDetails.OvertimeHoursWorked}\n" +
                                    $"Overtime Hourly Rate: {claimDetails.OvertimeHourlyRate}\n" +
                                    $"Start Date: {claimDetails.StartDate.ToShortDateString()}\n" +
                                    $"End Date: {claimDetails.EndDate.ToShortDateString()}\n" +
                                    $"Start Time: {claimDetails.StartTime}\n" +
                                    $"Additional Notes: {claimDetails.AdditionalNotes}\n" +
                                    $"Claim Money Amount: {claimMoneyAmount:C}"; // Format as currency

            // Show the details message
            MessageBox.Show(detailsMessage, "Claim Details", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the method to check for attached files
            CheckForAttachedFile(claimId);
        }


        private void CheckForAttachedFile(int claimId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT UploadedFile FROM ClaimsRecord WHERE ClaimId = @ClaimId";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimId", claimId);
                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        currentFileInfo = result.ToString();
                        FileInfoText.Text = "File attached to claim";
                        DownloadButton.IsEnabled = true;
                    }
                    else
                    {
                        currentFileInfo = null;
                        FileInfoText.Text = "No file attached";
                        DownloadButton.IsEnabled = false;
                    }
                }
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileInfo == null || !selectedClaimId.HasValue)
            {
                MessageBox.Show("No file available for download.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = $"claim_{selectedClaimId}_file.txt",
                    DefaultExt = ".txt",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 1,
                    Title = "Save Text File"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT UploadedFile FROM ClaimsRecord WHERE ClaimId = @ClaimId";

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ClaimId", selectedClaimId.Value);

                            object result = command.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                if (result is byte[] fileData)
                                {
                                    string content = System.Text.Encoding.UTF8.GetString(fileData);
                                    File.WriteAllText(saveFileDialog.FileName, content);

                                    MessageBox.Show("File downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Invalid file data format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("No file data found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedClaimId.HasValue)
            {
                MessageBox.Show("Please select a claim first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateClaimStatus(selectedClaimId.Value, "Approved");
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedClaimId.HasValue)
            {
                MessageBox.Show("Please select a claim first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateClaimStatus(selectedClaimId.Value, "Rejected");
        }

        private void UpdateClaimStatus(int claimId, string status)
        {
            string rejectionReason = null;

            if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                // Show input dialog to get rejection reason from admin
                rejectionReason = Microsoft.VisualBasic.Interaction.InputBox("Please enter the rejection reason:", "Rejection Reason", "");
            }

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string updateQuery = "UPDATE ClaimsRecord SET Status = @Status, RejectionReason = @RejectionReason WHERE ClaimId = @ClaimId";

                using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@ClaimId", claimId);

                    // Add rejection reason only if the claim is being rejected
                    if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        command.Parameters.AddWithValue("@RejectionReason", rejectionReason ?? (object)DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@RejectionReason", DBNull.Value);
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                        MessageBox.Show($"Claim {claimId} has been {status.ToLower()}.\nReason: {rejectionReason}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCurrentClaims();
                        LoadStatusHistory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating claim status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }




        private ClaimsRecord GetClaimDetailsById(int claimId)
        {
            ClaimsRecord claim = null;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ClaimsRecord WHERE ClaimId = @ClaimId";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimId", claimId);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            claim = new ClaimsRecord
                            {
                                ClaimId = reader.GetInt32(0),
                                LecturerId = reader.GetInt32(1),
                                LecturerName = reader.GetString(2),
                                LecturerSurname = reader.GetString(3),
                                HourlyRate = reader.GetDecimal(4),
                                OvertimeHoursWorked = reader.GetInt32(5),
                                OvertimeHourlyRate = reader.GetDecimal(6),
                                StartDate = reader.GetDateTime(7),
                                EndDate = reader.GetDateTime(8),
                                AdditionalNotes = reader.IsDBNull(11) ? null : reader.GetString(11),
                            };
                        }
                    }
                }
            }
            return claim;
        }
    }
}