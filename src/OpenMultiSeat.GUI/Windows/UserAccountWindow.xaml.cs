using System.Management;
using System.Windows;
using System.Windows.Controls;
using OpenMultiSeat.Core;

namespace OpenMultiSeat.GUI.Windows;

/// <summary>
/// ASTER's "User Account for Workstation" dialog: assigns a Windows login (local or domain
/// account) to a seat, defaulting to "Display login dialog" (no auto-login) exactly like ASTER's
/// own default. A password, if one is entered, is protected with Windows DPAPI
/// (CurrentUser scope) and stored base64-encoded on the Seat — never in plaintext.
///
/// Deliberately scoped: this dialog only configures the login (data + real encrypted storage).
/// It does NOT wire up actual unattended auto-login / session launch (CreateProcessWithLogonW or
/// equivalent) — that's a materially bigger, more security-sensitive piece (impersonation, logon
/// session lifecycle) than a configuration dialog, and isn't rushed in here. See
/// docs/control-panel/user-account-for-workstation.md for the honest status of what's real vs.
/// still needed.
/// </summary>
public partial class UserAccountWindow : Window
{
    private const string DisplayLoginDialogSentinel = "Display login dialog";

    private readonly ISeatManager _seatManager;
    private readonly Seat _seat;

    public UserAccountWindow(ISeatManager seatManager, Seat seat)
    {
        InitializeComponent();
        _seatManager = seatManager;
        _seat = seat;

        TitleText.Text = $"User Account — {seat.Name}";

        Loaded += (_, _) => LoadLocalAccounts();
    }

    private void LoadLocalAccounts()
    {
        var accounts = new List<string> { DisplayLoginDialogSentinel };

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name FROM Win32_UserAccount WHERE LocalAccount=True AND Disabled=False");
            foreach (ManagementBaseObject result in searcher.Get())
            {
                using var account = result;
                if (account["Name"] is string name && !string.IsNullOrWhiteSpace(name))
                    accounts.Add(name);
            }
        }
        catch
        {
            // WMI unavailable/restricted — the dropdown just falls back to "Display login
            // dialog" plus whatever the seat already had configured.
        }

        if (!string.IsNullOrEmpty(_seat.WindowsUser) && _seat.WindowsDomain == null && !accounts.Contains(_seat.WindowsUser))
            accounts.Add(_seat.WindowsUser);

        LocalAccountComboBox.ItemsSource = accounts;

        if (_seat.WindowsDomain != null)
        {
            DomainRadio.IsChecked = true;
            DomainTextBox.Text = _seat.WindowsDomain;
            DomainUserTextBox.Text = _seat.WindowsUser ?? string.Empty;
        }
        else
        {
            var selected = _seat.DisplayLoginDialog || string.IsNullOrEmpty(_seat.WindowsUser)
                ? DisplayLoginDialogSentinel
                : _seat.WindowsUser;
            LocalAccountComboBox.SelectedItem = accounts.Contains(selected) ? selected : DisplayLoginDialogSentinel;
        }

        UpdatePasswordFieldsEnabled();
    }

    private void OnAccountTypeChanged(object sender, RoutedEventArgs e)
    {
        if (LocalPanel == null || DomainPanel == null)
            return; // fires during InitializeComponent before the panels exist yet

        LocalPanel.Visibility = LocalRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        DomainPanel.Visibility = DomainRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        UpdatePasswordFieldsEnabled();
    }

    private void OnLocalAccountChanged(object sender, SelectionChangedEventArgs e) => UpdatePasswordFieldsEnabled();

    private void UpdatePasswordFieldsEnabled()
    {
        var displayLoginDialogSelected = LocalRadio.IsChecked == true
            && LocalAccountComboBox.SelectedItem as string == DisplayLoginDialogSentinel;

        PasswordBox.IsEnabled = !displayLoginDialogSelected;
        PasswordConfirmBox.IsEnabled = !displayLoginDialogSelected;

        if (displayLoginDialogSelected)
        {
            PasswordBox.Password = string.Empty;
            PasswordConfirmBox.Password = string.Empty;
        }
    }

    private async void OnOk(object sender, RoutedEventArgs e)
    {
        var displayLoginDialog = LocalRadio.IsChecked == true
            && LocalAccountComboBox.SelectedItem as string == DisplayLoginDialogSentinel;

        string? username;
        string? domain;

        if (DomainRadio.IsChecked == true)
        {
            domain = DomainTextBox.Text.Trim();
            username = DomainUserTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(username))
            {
                ShowError("Enter both a domain name and a username.");
                return;
            }
        }
        else
        {
            domain = null;
            username = displayLoginDialog ? null : LocalAccountComboBox.SelectedItem as string;
        }

        string? encryptedPassword = _seat.EncryptedPassword;

        if (!displayLoginDialog)
        {
            var password = PasswordBox.Password;
            var confirmation = PasswordConfirmBox.Password;

            if (password != confirmation)
            {
                ShowError("Password and confirmation don't match.");
                return;
            }

            if (!string.IsNullOrEmpty(password))
            {
                try
                {
                    encryptedPassword = SeatCredentialProtector.Protect(password);
                }
                catch (Exception ex)
                {
                    ShowError($"Couldn't encrypt the password: {ex.Message}");
                    return;
                }
            }
        }
        else
        {
            encryptedPassword = null;
        }

        _seat.WindowsUser = username;
        _seat.WindowsDomain = domain;
        _seat.DisplayLoginDialog = displayLoginDialog;
        _seat.EncryptedPassword = encryptedPassword;

        try
        {
            await _seatManager.UpdateSeatAsync(_seat);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Couldn't save the account settings: {ex.Message}");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
