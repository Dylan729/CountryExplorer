using System.Net.Http.Headers;
using System.Text.Json;
using CountryExplorer.Models;

namespace CountryExplorer.Services
{
    // Wraps any failure (network, timeout, bad status, bad JSON) so pages
    // can show one friendly message without leaking technical detail (Part 16).
    public class CountryServiceException : Exception
    {
        public CountryServiceException(string message) : base(message) { }
    }

    public class CountryService
    {
        private readonly HttpClient _httpClient;

        // NOTE: hardcoding an API key like this is fine for a throwaway
        // learning project. For anything real, pull it from an untracked
        // config value or environment variable instead of committing it
        // to source control.
        private const string ApiKey = "rc_live_e0d2d3696e774fcca4c6656f4b2a9c2f";

        private const string BaseUrl = "https://api.restcountries.com/countries/v5";
        private const string Fields =
            "names.common,names.official,capitals,region,subregion,population,flag.url_png,currencies,languages";

        private const int PageSize = 100; // max allowed on the free plan
        private const int MaxPages = 5;   // safety cap - covers all ~249 countries

        public CountryService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            var allCountries = new List<Country>();

            try
            {
                int offset = 0;

                for (int page = 0; page < MaxPages; page++)
                {
                    var url = $"{BaseUrl}?response_fields={Fields}&limit={PageSize}&offset={offset}";
                    var response = await _httpClient.GetAsync(url);
                    var body = await response.Content.ReadAsStringAsync();

                    JsonDocument doc;
                    try
                    {
                        doc = JsonDocument.Parse(body);
                    }
                    catch (JsonException)
                    {
                        throw new CountryServiceException("The country data could not be read.");
                    }

                    using (doc)
                    {
                        var root = doc.RootElement;

                        if (!response.IsSuccessStatusCode)
                        {
                            string? message = TryGetErrorMessage(root);
                            throw new CountryServiceException(message ??
                                $"The server responded with status {(int)response.StatusCode}.");
                        }

                        if (!root.TryGetProperty("data", out var data)
                            || !data.TryGetProperty("objects", out var objects)
                            || objects.ValueKind != JsonValueKind.Array)
                        {
                            throw new CountryServiceException("No country records were returned.");
                        }

                        foreach (var obj in objects.EnumerateArray())
                        {
                            allCountries.Add(ParseCountry(obj));
                        }

                        bool more = false;
                        if (data.TryGetProperty("meta", out var meta)
                            && meta.TryGetProperty("more", out var moreEl)
                            && moreEl.ValueKind == JsonValueKind.True)
                        {
                            more = true;
                        }

                        if (!more)
                            break;

                        offset += PageSize;
                    }
                }

                if (allCountries.Count == 0)
                {
                    throw new CountryServiceException("No country records were returned.");
                }

                return allCountries.OrderBy(c => c.CommonName).ToList();
            }
            catch (CountryServiceException)
            {
                throw; // already a friendly, known error - pass it through as-is
            }
            catch (TaskCanceledException)
            {
                throw new CountryServiceException(
                    "The request timed out. Please check your connection and try again.");
            }
            catch (HttpRequestException)
            {
                throw new CountryServiceException(
                    "Could not reach the server. Please check your connection and try again.");
            }
        }

        private static string? TryGetErrorMessage(JsonElement root)
        {
            if (root.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array
                && errors.GetArrayLength() > 0
                && errors[0].TryGetProperty("message", out var msgEl)
                && msgEl.ValueKind == JsonValueKind.String)
            {
                return msgEl.GetString();
            }

            return null;
        }

        // Reads each field defensively: a field that's missing or shaped
        // differently than expected is skipped rather than throwing, so
        // one unexpected nested shape doesn't take down the whole page.
        private static Country ParseCountry(JsonElement el)
        {
            var country = new Country();

            if (el.TryGetProperty("names", out var names) && names.ValueKind == JsonValueKind.Object)
            {
                if (names.TryGetProperty("common", out var common) && common.ValueKind == JsonValueKind.String)
                    country.CommonName = common.GetString() ?? "Unknown";

                if (names.TryGetProperty("official", out var official) && official.ValueKind == JsonValueKind.String)
                    country.OfficialName = official.GetString() ?? string.Empty;
            }

            if (el.TryGetProperty("region", out var region) && region.ValueKind == JsonValueKind.String)
                country.Region = region.GetString() ?? string.Empty;

            if (el.TryGetProperty("subregion", out var subregion) && subregion.ValueKind == JsonValueKind.String)
                country.Subregion = subregion.GetString();

            if (el.TryGetProperty("population", out var population)
                && population.ValueKind == JsonValueKind.Number
                && population.TryGetInt64(out var pop))
            {
                country.Population = pop;
            }

            if (el.TryGetProperty("flag", out var flag) && flag.ValueKind == JsonValueKind.Object
                && flag.TryGetProperty("url_png", out var flagUrl) && flagUrl.ValueKind == JsonValueKind.String)
            {
                country.FlagUrl = flagUrl.GetString() ?? string.Empty;
            }

            if (el.TryGetProperty("capitals", out var capitals) && capitals.ValueKind == JsonValueKind.Array)
            {
                foreach (var cap in capitals.EnumerateArray())
                {
                    string? value = cap.ValueKind switch
                    {
                        JsonValueKind.String => cap.GetString(),
                        JsonValueKind.Object when cap.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                            => n.GetString(),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(value))
                        country.Capitals.Add(value!);
                }
            }

            if (el.TryGetProperty("currencies", out var currencies) && currencies.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in currencies.EnumerateObject())
                {
                    string name = prop.Name;
                    string symbol = string.Empty;

                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        if (prop.Value.TryGetProperty("name", out var cn) && cn.ValueKind == JsonValueKind.String)
                            name = cn.GetString() ?? prop.Name;

                        if (prop.Value.TryGetProperty("symbol", out var cs) && cs.ValueKind == JsonValueKind.String)
                            symbol = cs.GetString() ?? string.Empty;
                    }

                    country.Currencies.Add(string.IsNullOrEmpty(symbol) ? name : $"{name} ({symbol})");
                }
            }

            if (el.TryGetProperty("languages", out var languages) && languages.ValueKind == JsonValueKind.Array)
            {
                var candidateKeys = new[] { "name", "english_name", "native_name", "bcp47" };

                foreach (var lang in languages.EnumerateArray())
                {
                    string? display = lang.ValueKind switch
                    {
                        JsonValueKind.String => lang.GetString(),
                        _ => null
                    };

                    if (display == null && lang.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var key in candidateKeys)
                        {
                            if (lang.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                            {
                                var s = v.GetString();
                                if (!string.IsNullOrWhiteSpace(s))
                                {
                                    display = s;
                                    break;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(display))
                        country.Languages.Add(display!);
                }
            }

            return country;
        }
    }
}