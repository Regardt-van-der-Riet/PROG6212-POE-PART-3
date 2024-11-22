using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace CMCS
{
    public partial class Landing : Page
    {
        private string connectionString = "Data Source=DataFile3.db;Version=3;";

        public Landing()
        {
            InitializeComponent();
            LoadCurrentClaims();
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

        private void ClaimsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClaimsListBox.SelectedItem != null)
            {
                // Parse ClaimId from the selected item string
                string selectedItem = ClaimsListBox.SelectedItem.ToString();
                int claimIdStartIndex = selectedItem.IndexOf("ID: ") + 4;
                int claimIdEndIndex = selectedItem.IndexOf(",", claimIdStartIndex);
                string claimIdString = selectedItem.Substring(claimIdStartIndex, claimIdEndIndex - claimIdStartIndex);

                if (int.TryParse(claimIdString, out int selectedClaimId))
                {
                    ShowClaimDetails(selectedClaimId);
                }

                ClaimsListBox.SelectedItem = null; // Reset selection
            }
        }

        private void ShowClaimDetails(int claimId)
        {
            ClaimsRecord claimDetails = GetClaimDetailsById(claimId);

            if (claimDetails == null)
            {
                MessageBox.Show("Claim details not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calculate the claim money amount
            decimal claimMoneyAmount = claimDetails.OvertimeHoursWorked * claimDetails.OvertimeHourlyRate;

            // Determine the status display
            string statusDisplay = string.IsNullOrEmpty(claimDetails.Status) ? "Pending" : claimDetails.Status;

            // Build the details message
            string detailsMessage = $"Claim ID: {claimDetails.ClaimId}\n" +
                                    $"Lecturer Name: {claimDetails.LecturerName} {claimDetails.LecturerSurname}\n" +
                                    $"Hourly Rate: {claimDetails.HourlyRate:C}\n" + // Format as currency
                                    $"Overtime Hours Worked: {claimDetails.OvertimeHoursWorked}\n" +
                                    $"Overtime Hourly Rate: {claimDetails.OvertimeHourlyRate:C}\n" + // Format as currency
                                    $"Start Date: {claimDetails.StartDate.ToShortDateString()}\n" +
                                    $"End Date: {claimDetails.EndDate.ToShortDateString()}\n" +
                                    $"Start Time: {claimDetails.StartTime}\n" +
                                    $"Additional Notes: {claimDetails.AdditionalNotes}\n" +
                                    $"Status: {statusDisplay}\n" +
                                    $"Claim Money Amount: {claimMoneyAmount:C}\n"; // Format as currency

            // Append rejection reason if the status is "Rejected"
            if (statusDisplay.Equals("Rejected", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(claimDetails.RejectionReason))
            {
                detailsMessage += $"Reason for Rejection: {claimDetails.RejectionReason}\n";
            }

            MessageBox.Show(detailsMessage, "Claim Details", MessageBoxButton.OK, MessageBoxImage.Information);
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
                                Status = reader.IsDBNull(10) ? null : reader.GetString(10),
                            };
                        }
                    }
                }
            }

            return claim;
        }

        private void FileClaimButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Claims());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Login());
        }
    }
}
