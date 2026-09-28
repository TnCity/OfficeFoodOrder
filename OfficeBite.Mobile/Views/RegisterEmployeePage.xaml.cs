using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class RegisterEmployeePage : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterEmployeePage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void BackButton_Clicked(object sender, EventArgs e) =>
        await Navigation.PopAsync();

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopAsync();
        return true;
    }

    private async void SignInLink_Clicked(object sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        HideError();

        var fullName = FullNameEntry.Text?.Trim();
        var mobile   = MobileEntry.Text?.Trim();
        var email    = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirm  = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Please enter your full name.");
            FullNameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(mobile))
        {
            ShowError("Please enter your mobile number.");
            MobileEntry.Focus();
            return;
        }

        if (mobile.Length < 10)
        {
            ShowError("Please enter a valid 10-digit mobile number.");
            MobileEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
        {
            ShowError("Please enter a valid email address.");
            EmailEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter a password.");
            PasswordEntry.Focus();
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password must be at least 6 characters long.");
            PasswordEntry.Focus();
            return;
        }

        if (password != confirm)
        {
            ShowError("Passwords do not match. Please re-check.");
            ConfirmPasswordEntry.Focus();
            return;
        }

        SetLoading(true);

        try
        {
            var req = new RegisterRequest
            {
                FullName = fullName,
                Mobile   = mobile,
                Email    = email,
                Password = password
            };

            var (success, message) = await _apiService.RegisterAsync(req);

            if (success)
            {
                await DisplayAlert("Success!", $"Account created successfully for {fullName}!\nYou can now sign in with your email and password.", "Sign In Now");
                await Navigation.PopAsync();
            }
            else
            {
                ShowError(string.IsNullOrWhiteSpace(message) ? "Registration failed. Please try again." : message);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorBorder.IsVisible = true;
    }

    private void HideError()
    {
        ErrorBorder.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    private void SetLoading(bool loading)
    {
        RegisterButton.IsEnabled   = !loading;
        LoadingIndicator.IsVisible = loading;
        LoadingIndicator.IsRunning = loading;
    }
}
