using CountryExplorer.Models;

namespace CountryExplorer.Views;

public partial class CountryDetailsPage : ContentPage
{
    public CountryDetailsPage(Country country)
    {
        InitializeComponent();

        FlagImage.Source = country.FlagUrl;
        CommonNameLabel.Text = country.CommonName;
        OfficialNameLabel.Text = country.OfficialName;
        CapitalLabel.Text = country.CapitalDisplay;
        RegionLabel.Text = country.Region;
        SubregionLabel.Text = string.IsNullOrWhiteSpace(country.Subregion) ? "N/A" : country.Subregion;
        PopulationLabel.Text = country.PopulationDisplay;
        CurrenciesLabel.Text = country.CurrenciesDisplay;
        LanguagesLabel.Text = country.LanguagesDisplay;
    }
}