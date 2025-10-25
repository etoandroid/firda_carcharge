using System.Globalization;
using System.Text.RegularExpressions;

namespace imicCharge.APP.Controls;

public partial class TopUpModal : ContentView
{
    // TaskCompletionSource returns the valid amount or null if canceled/invalid initially
    private TaskCompletionSource<decimal?>? _tcs;

    public string AmountText => AmountEntry.Text;
    
    public TopUpModal()
    {
        InitializeComponent();
    }

    public async Task<decimal?> Show(string initialValue = "")
    {
        AmountEntry.Text = initialValue; // Pre-fill if needed
        _tcs = new TaskCompletionSource<decimal?>();

        IsVisible = true;
        InputTransparent = false;
        _ = this.FadeTo(1, 250, Easing.CubicOut);

        await Task.Delay(100); // Small delay to ensure visibility before focusing
        AmountEntry.Focus();

        decimal? result = await _tcs.Task; // Wait for Pay or Cancel button to be clicked

        return result;
    }

    // Public method to hide the top-up modal
    public async Task Hide(bool clearEntry = false)
    {
        if (!IsVisible) return;

        await this.FadeTo(0, 250, Easing.CubicIn);
        this.IsVisible = false;
        this.InputTransparent = true;
        if (clearEntry)
        {
            AmountEntry.Text = string.Empty;
        }
    }

    // Pay Button Clicked
    private void OnPayButtonClicked(object? sender, EventArgs e)
    {
        if (decimal.TryParse(AmountEntry.Text.Replace('.', ','), NumberStyles.Any, CultureInfo.GetCultureInfo("nb-NO"), out decimal amount))
        {
            _tcs?.SetResult(amount);
        }
        else
        {
            _tcs?.SetResult(null);
        }
    }

    // Cancel Button Clicked
    private async void OnCancelButtonClicked(object sender, EventArgs e)
    {
        await Hide(clearEntry: true); // Hide and clear entry
        _tcs?.SetResult(null); // Return null to indicate cancellation
    }

    // Filter input (allow only digits and one comma or period)
    private void AmountEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;

        string filtered = Regex.Replace(e.NewTextValue, @"[^0-9.,]", "");

        // Allow only one decimal separator (prefer comma)
        int commaCount = filtered.Count(c => c == ',');
        int periodCount = filtered.Count(c => c == '.');

        if (commaCount > 1 || periodCount > 1 || (commaCount == 1 && periodCount == 1))
        {
            // Too many separators, revert to old value
            ((Entry)sender).Text = e.OldTextValue ?? "";
            return;
        }

        // Auto-replace period with comma
        filtered = filtered.Replace('.', ',');

        // Update text if different
        if (((Entry)sender).Text != filtered)
        {
            ((Entry)sender).Text = filtered;
        }
    }

    // Handle "Enter" / "Go" key on the amount entry
    private void AmountEntry_Completed(object? sender, EventArgs e)
    {
        // Trigger the same logic as the pay button
        OnPayButtonClicked(AmountEntry, e);
    }
}