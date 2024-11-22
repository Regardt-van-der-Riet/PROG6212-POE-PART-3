using System;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Text;
using System.Collections.Generic;

namespace CMCS
{
    public partial class Claims : Page
    {
        private string _selectedFilePath;
        private byte[] _fileData;
        private string _fileName;

        public Claims()
        {
            InitializeComponent();
        }

        public class ClaimsValidator
        {
            public class ValidationResult
            {
                public bool IsValid { get; set; }
                public List<string> ValidationMessages { get; set; }

                public ValidationResult()
                {
                    ValidationMessages = new List<string>();
                }
            }

            public static ValidationResult ValidateClaim(
                string lecturerId,
                string lecturerName,
                string lecturerSurname,
                string hourlyRate,
                string overtimeHoursWorked,
                string overtimeHourlyRate,
                DateTime? startDate,
                DateTime? endDate,
                string startHour,
                string startMinute,
                byte[] fileData)
            {
                var result = new ValidationResult { IsValid = true };

                // Required fields validation
                if (string.IsNullOrWhiteSpace(lecturerId) ||
                    string.IsNullOrWhiteSpace(lecturerName) ||
                    string.IsNullOrWhiteSpace(lecturerSurname))
                {
                    result.ValidationMessages.Add("All lecturer information fields are required.");
                    result.IsValid = false;
                }

                // Numeric validations
                if (!int.TryParse(lecturerId, out int lectId) || lectId <= 0)
                {
                    result.ValidationMessages.Add("Lecturer ID must be a positive number.");
                    result.IsValid = false;
                }

                if (!decimal.TryParse(hourlyRate, out decimal rate) || rate <= 0)
                {
                    result.ValidationMessages.Add("Hourly rate must be a positive number.");
                    result.IsValid = false;
                }

                if (!decimal.TryParse(overtimeHoursWorked, out decimal overtimeHours) || overtimeHours < 0)
                {
                    result.ValidationMessages.Add("Overtime hours must be zero or greater.");
                    result.IsValid = false;
                }

                if (!decimal.TryParse(overtimeHourlyRate, out decimal overtimeRate) || overtimeRate <= 0)
                {
                    result.ValidationMessages.Add("Overtime hourly rate must be a positive number.");
                    result.IsValid = false;
                }

                // Date validations
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    result.ValidationMessages.Add("Both start and end dates are required.");
                    result.IsValid = false;
                }
                else
                {
                    if (startDate.Value > endDate.Value)
                    {
                        result.ValidationMessages.Add("Start date cannot be after end date.");
                        result.IsValid = false;
                    }

                    if (startDate.Value > DateTime.Now || endDate.Value > DateTime.Now)
                    {
                        result.ValidationMessages.Add("Dates cannot be in the future.");
                        result.IsValid = false;
                    }
                }

                // Time validations
                if (string.IsNullOrEmpty(startHour) || string.IsNullOrEmpty(startMinute))
                {
                    result.ValidationMessages.Add("Start time is required.");
                    result.IsValid = false;
                }
                else
                {
                    if (!int.TryParse(startHour, out int hour) || hour < 0 || hour > 23)
                    {
                        result.ValidationMessages.Add("Invalid hour selected.");
                        result.IsValid = false;
                    }

                    if (!int.TryParse(startMinute, out int minute) || minute < 0 || minute > 59)
                    {
                        result.ValidationMessages.Add("Invalid minute selected.");
                        result.IsValid = false;
                    }
                }

                // Name validations
                if (lecturerName?.Length < 2 || lecturerSurname?.Length < 2)
                {
                    result.ValidationMessages.Add("Names must be at least 2 characters long.");
                    result.IsValid = false;
                }

                // Business rule validations
                if (overtimeHours > 40)
                {
                    result.ValidationMessages.Add("Overtime hours cannot exceed 40 hours per claim.");
                    result.IsValid = false;
                }

                if (rate > 1000)
                {
                    result.ValidationMessages.Add("Hourly rate cannot exceed 1000 per hour.");
                    result.IsValid = false;
                }

                // File validation
                if (fileData == null)
                {
                    result.ValidationMessages.Add("Supporting document is required.");
                    result.IsValid = false;
                }

                return result;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word files (*.doc;*.docx)|*.doc;*.docx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _selectedFilePath = openFileDialog.FileName;
                    _fileName = Path.GetFileName(_selectedFilePath);

                    _fileData = File.ReadAllBytes(_selectedFilePath);

                    if (_fileData.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show("File is too large. Please select a file smaller than 10MB.",
                            "File Size Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _fileData = null;
                        _selectedFilePath = null;
                        return;
                    }

                    MessageBox.Show($"File selected: {_fileName}", "File Upload",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}", "File Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _fileData = null;
                    _selectedFilePath = null;
                }
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string lecturerId = LecturerID.Text;
            string lecturerName = LecturerName.Text;
            string lecturerSurname = LecturerSurname.Text;
            string hourlyRate = HourlyRate.Text;
            string overtimeHoursWorked = OvertimeHoursWorked.Text;
            string overtimeHourlyRate = OvertimeHourlyRate.Text;
            DateTime? startDate = StartDate.SelectedDate;
            DateTime? endDate = EndDate.SelectedDate;
            string startHour = (StartHour.SelectedItem as ComboBoxItem)?.Content.ToString();
            string startMinute = (StartMinute.SelectedItem as ComboBoxItem)?.Content.ToString();
            string additionalNotes = AdditionalNotes.Text;

            // Perform validation
            var validationResult = ClaimsValidator.ValidateClaim(
                lecturerId,
                lecturerName,
                lecturerSurname,
                hourlyRate,
                overtimeHoursWorked,
                overtimeHourlyRate,
                startDate,
                endDate,
                startHour,
                startMinute,
                _fileData
            );

            if (!validationResult.IsValid)
            {
                // Build error message
                StringBuilder errorMessage = new StringBuilder("Please correct the following issues:\n\n");
                foreach (var message in validationResult.ValidationMessages)
                {
                    errorMessage.AppendLine("• " + message);
                }

                MessageBox.Show(errorMessage.ToString(),
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // If validation passes, proceed with saving
            try
            {
                var claim = new ClaimsRecord
                {
                    LecturerId = int.Parse(lecturerId),
                    LecturerName = lecturerName,
                    LecturerSurname = lecturerSurname,
                    HourlyRate = decimal.Parse(hourlyRate),
                    OvertimeHoursWorked = int.Parse(overtimeHoursWorked),
                    OvertimeHourlyRate = decimal.Parse(overtimeHourlyRate),
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    StartTime = new TimeSpan(int.Parse(startHour), int.Parse(startMinute), 0),
                    AdditionalNotes = additionalNotes,
                    UploadedFile = _fileData
                };

                using (var context = new ClaimsData())
                {
                    context.ClaimsRecord.Add(claim);
                    context.SaveChanges();
                }

                MessageBox.Show("Claim validated and submitted successfully.",
                    "Submission Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _fileData = null;
                _selectedFilePath = null;

                // Clear the form after successful submission
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving claim: {ex.Message}",
                    "Submission Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            LecturerID.Text = string.Empty;
            LecturerName.Text = string.Empty;
            LecturerSurname.Text = string.Empty;
            HourlyRate.Text = string.Empty;
            OvertimeHoursWorked.Text = string.Empty;
            OvertimeHourlyRate.Text = string.Empty;
            StartDate.SelectedDate = null;
            EndDate.SelectedDate = null;
            StartHour.SelectedIndex = -1;
            StartMinute.SelectedIndex = -1;
            AdditionalNotes.Text = string.Empty;
        }
    }
}