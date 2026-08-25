using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Catalyst_Launcher.Services;

namespace Catalyst_Launcher;

public partial class LoginWindow : Window
{
    private bool _isBusy;

    public AuthUserInfo? LoggedInUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TabLogin_Checked(object sender, RoutedEventArgs e)
    {
        if (LoginPanel is null) return;
        LoginPanel.Visibility = Visibility.Visible;
        RegisterPanel.Visibility = Visibility.Collapsed;
        ClearStatus();
    }

    private void TabRegister_Checked(object sender, RoutedEventArgs e)
    {
        if (RegisterPanel is null) return;
        RegisterPanel.Visibility = Visibility.Visible;
        LoginPanel.Visibility = Visibility.Collapsed;
        ClearStatus();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        SetBusy(true, LoginBtn, "Signing in…");

        string email = LoginEmail.Text.Trim();
        string password = LoginPassword.Password;

        var (result, user) = await LoginService.LoginAsync(email, password);

        SetBusy(false, LoginBtn, "Sign In");

        switch (result)
        {
            case LoginResult.Success:
                LoggedInUser = user;
                DialogResult = true;
                break;
            case LoginResult.EmptyFields:
                ShowStatus("Please enter your email and password.", isError: true);
                break;
            case LoginResult.InvalidCredentials:
                ShowStatus("Incorrect email or password.", isError: true);
                break;
            default:
                ShowStatus("Could not reach the server. Check your connection.", isError: true);
                break;
        }
    }


    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        string username = RegUsername.Text.Trim();
        string email    = RegEmail.Text.Trim();
        string password = RegPassword.Password;
        string confirm  = RegConfirmPassword.Password;

        if (password != confirm)
        {
            ShowStatus("Passwords do not match.", isError: true);
            return;
        }

        SetBusy(true, RegisterBtn, "Creating account…");

        var (result, user) = await LoginService.RegisterAsync(username, email, password);

        SetBusy(false, RegisterBtn, "Create Account");

        switch (result)
        {
            case LoginResult.Success:
                LoggedInUser = user;
                DialogResult = true;
                break;
            case LoginResult.EmptyFields:
                ShowStatus("Please fill in all fields.", isError: true);
                break;
            case LoginResult.UserExists:
                ShowStatus("An account with that email already exists.", isError: true);
                break;
            default:
                ShowStatus("Could not reach the server. Check your connection.", isError: true);
                break;
        }
    }

    private void ContinueOffline_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        //TODO: Add password Reset
        ShowStatus("Password reset is not yet implemented.", isError: false);
    }

    
    private void Password_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return) return;
        if (LoginPanel.Visibility == Visibility.Visible)
            Login_Click(sender, e);
        else
            Register_Click(sender, e);
    }


    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SetBusy(bool busy, System.Windows.Controls.Button btn, string label)
    {
        _isBusy = busy;
        btn.Content = label;
        btn.IsEnabled = !busy;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.Foreground = (Brush)FindResource(isError ? "RedlineBrush" : "VerdigrisBrush");
        StatusFrame.BorderBrush = (Brush)FindResource(isError ? "RedlineBrush" : "RuleBrush");
        StatusFrame.Visibility = Visibility.Visible;
    }

    private void ClearStatus()
    {
        StatusFrame.Visibility = Visibility.Collapsed;
        StatusLabel.Text = string.Empty;
    }
}
