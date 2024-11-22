using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CMCS
{
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var Username = UsernameTextBox.Text;
            var Password = PasswordBox.Password;
            var loginType = (LoginTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (loginType == "Admin")
            {
                using (AdminData context = new AdminData())
                {
                    bool adminFound = context.Admin.Any(admin => admin.AdminName == Username && admin.Password == Password);

                    if (adminFound)
                    {
                        GrantAccess("Admin");
                    }
                    else
                    {
                        MessageBox.Show("Admin Not Found");
                    }
                }
            }
            else if (loginType == "Lecturer")
            {
                using (UserData context = new UserData())
                {
                    bool userFound = context.User.Any(user => user.UserName == Username && user.Password == Password);

                    if (userFound)
                    {
                        GrantAccess("Lecturer");
                    }
                    else
                    {
                        MessageBox.Show("User Not Found");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a valid login type.");
            }
        }

        private void GrantAccess(string loginType)
        {
            if (loginType == "Admin")
            {
                NavigationService.Navigate(new Uri("AdminSystem.xaml", UriKind.Relative));
            }
            else if (loginType == "Lecturer")
            {
                NavigationService.Navigate(new Uri("Landing.xaml", UriKind.Relative));
            }
        }
    }
}
