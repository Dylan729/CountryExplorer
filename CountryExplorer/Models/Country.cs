namespace CountryExplorer.Models
{
    // Plain POCO - built manually by CountryService from the raw JSON,
    // rather than relying on System.Text.Json's strict attribute-based
    // binding, since the v5 API's exact nested shapes for currencies/
    // languages/capitals aren't fully pinned down from documentation alone.
    public class Country
    {
        public string CommonName { get; set; } = "Unknown";
        public string OfficialName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string? Subregion { get; set; }
        public long? Population { get; set; }
        public string FlagUrl { get; set; } = string.Empty;
        public List<string> Capitals { get; set; } = new();
        public List<string> Currencies { get; set; } = new();
        public List<string> Languages { get; set; } = new();

        public string CapitalDisplay => Capitals.Count > 0 ? string.Join(", ", Capitals) : "N/A";
        public string PopulationDisplay => Population.HasValue ? Population.Value.ToString("N0") : "N/A";
        public string CurrenciesDisplay => Currencies.Count > 0 ? string.Join(", ", Currencies) : "N/A";
        public string LanguagesDisplay => Languages.Count > 0 ? string.Join(", ", Languages) : "N/A";
    }
}