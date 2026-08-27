using CountryExplorer.Models;
using CountryExplorer.Services;

namespace CountryExplorer.Views;

public partial class CountriesPage : ContentPage
{
    private readonly CountryService _service = new CountryService();
    private List<Country> _allCountries = new();
    private bool _hasLoadedOnce;

    private static readonly string[] Regions =
    {
        "All Regions", "Africa", "Americas", "Asia", "Europe", "Oceania"
    };

    public CountriesPage()
    {
        InitializeComponent();
        RegionPicker.ItemsSource = Regions;
        RegionPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Only auto-load the first time the page appears, so coming back
        // from the details page doesn't trigger a fresh API call.
        if (!_hasLoadedOnce)
        {
            _hasLoadedOnce = true;
            await LoadCountriesAsync();
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadCountriesAsync();
    }

    private async Task LoadCountriesAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatusLabel.Text = "Loading countries...";
        CountriesCollectionView.ItemsSource = null;

        try
        {
            _allCountries = await _service.GetCountriesAsync();
            ApplyFilters();
        }
        catch (CountryServiceException ex)
        {
            // Friendly message only - no raw exception detail (Part 16).
            _allCountries = new List<Country>();
            CountriesCollectionView.ItemsSource = null;
            StatusLabel.Text = ex.Message;
        }
        catch
        {
            _allCountries = new List<Country>();
            CountriesCollectionView.ItemsSource = null;
            StatusLabel.Text = "Country information could not be loaded. Please check your connection and try again.";
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void OnRegionChanged(object sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allCountries.Count == 0)
            return;

        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var selectedRegion = RegionPicker.SelectedItem as string ?? "All Regions";

        IEnumerable<Country> filtered = _allCountries;

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(c =>
                c.CommonName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(selectedRegion) && selectedRegion != "All Regions")
        {
            filtered = filtered.Where(c =>
                string.Equals(c.Region, selectedRegion, StringComparison.OrdinalIgnoreCase));
        }

        var results = filtered.ToList();
        CountriesCollectionView.ItemsSource = results;

        StatusLabel.Text = results.Count == 0
            ? "No countries match your search."
            : $"Showing {results.Count} countr{(results.Count == 1 ? "y" : "ies")}";
    }

    private async void OnCountrySelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as Country;
        CountriesCollectionView.SelectedItem = null;

        if (selected == null)
            return;

        await Navigation.PushAsync(new CountryDetailsPage(selected));
    }
}