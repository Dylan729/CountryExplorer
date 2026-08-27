using CountryExplorer.Views;

namespace CountryExplorer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(CountriesPage), typeof(CountriesPage));
            Routing.RegisterRoute(nameof(CountryDetailsPage), typeof(CountryDetailsPage));
        }
    }
}