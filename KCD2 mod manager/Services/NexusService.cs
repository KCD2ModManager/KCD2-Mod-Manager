using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Http;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von INexusService für Nexus Mods API-Operationen
    /// </summary>
    public class NexusService : INexusService, INexusAuthService, INexusDownloadService, INexusModActionsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAppSettings _settings;
        private readonly ILog _logger;

        public NexusService(IHttpClientFactory httpClientFactory, IAppSettings settings, ILog logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _logger = logger;
        }

        public async Task<bool> StartNexusSSOAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string uuid = Guid.NewGuid().ToString();
                string token = null;
                string applicationSlug = "kcd2mm";

                using (var clientWebSocket = new ClientWebSocket())
                {
                    await clientWebSocket.ConnectAsync(new Uri("wss://sso.nexusmods.com"), cancellationToken);

                    var requestData = new
                    {
                        id = uuid,
                        token = token,
                        protocol = 2
                    };
                    string jsonRequest = JsonSerializer.Serialize(requestData);
                    var buffer = Encoding.UTF8.GetBytes(jsonRequest);

                    await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);

                    var receiveBuffer = new byte[1024];
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                    string responseJson = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                    string url = $"https://www.nexusmods.com/sso?id={uuid}&application={applicationSlug}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                    responseJson = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                    {
                        if (doc.RootElement.TryGetProperty("success", out JsonElement successElement) && successElement.GetBoolean())
                        {
                            if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                                dataElement.TryGetProperty("api_key", out JsonElement apiKeyElement))
                            {
                                string apiKey = apiKeyElement.GetString() ?? string.Empty;
                                _settings.NexusUserToken = apiKey;
                                _settings.Save();

                                await ValidateNexusUserAsync(apiKey, cancellationToken);
                                return true;
                            }
                        }
                    }

                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Completed", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Fehler bei Nexus SSO", ex);
            }
            return false;
        }

        public async Task<bool> ValidateNexusUserAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                string validateUrl = "https://api.nexusmods.com/v1/users/validate.json";
                var response = await client.GetAsync(validateUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    if (doc.RootElement.TryGetProperty("user_id", out JsonElement userIdElement))
                    {
                        _settings.NexusUserID = userIdElement.GetInt64();
                    }
                    if (doc.RootElement.TryGetProperty("name", out JsonElement nameElement))
                    {
                        _settings.NexusUsername = nameElement.GetString() ?? string.Empty;
                    }
                    if (doc.RootElement.TryGetProperty("email", out JsonElement emailElement))
                    {
                        _settings.NexusUserEmail = emailElement.GetString() ?? string.Empty;
                    }
                    if (doc.RootElement.TryGetProperty("is_premium", out JsonElement isPremiumElement))
                    {
                        _settings.NexusIsPremium = isPremiumElement.GetBoolean();
                    }
                    _settings.Save();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Fehler bei Nexus User-Validierung", ex);
                return false;
            }
        }

        public async Task<bool> EndorseModAsync(string gameDomain, int modId, string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                string endorseUrl = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modId}/endorse.json";
                HttpResponseMessage response = await client.PostAsync(endorseUrl, null, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Endorsen von Mod {modId}", ex);
                return false;
            }
        }

        public async Task<string?> GetLatestVersionAsync(string modPageUrl, CancellationToken cancellationToken = default)
        {
            const string githubApiUrl = "https://api.github.com/repos/KCD2ModManager/KCD2-Mod-Manager/releases/latest";
            
            try
            {
                _logger.Info("Starte Version-Abruf von GitHub API");
                
                using (var client = new HttpClient())
                {
                    // GitHub API erfordert User-Agent Header
                    client.DefaultRequestHeaders.Add("User-Agent", "KCD2-Mod-Manager/1.0");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    _logger.Info($"Requesting GitHub API: {githubApiUrl}");
                    
                    using (var request = new HttpRequestMessage(HttpMethod.Get, githubApiUrl))
                    {
                        var response = await client.SendAsync(request, cancellationToken);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            _logger.Error($"GitHub API returned status {response.StatusCode}: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode})");
                        }
                        
                        string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.Info($"GitHub API response received, length: {jsonContent?.Length ?? 0} characters");
                        
                        // Parse JSON response
                        using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                        {
                            JsonElement root = doc.RootElement;
                            
                            if (!root.TryGetProperty("tag_name", out JsonElement tagNameElement))
                            {
                                _logger.Warning("GitHub API response does not contain 'tag_name' field");
                                return null;
                            }
                            
                            string tagName = tagNameElement.GetString() ?? string.Empty;
                            
                            if (string.IsNullOrEmpty(tagName))
                            {
                                _logger.Warning("GitHub API 'tag_name' field is null or empty");
                                return null;
                            }
                            
                            // Remove leading "v" if present (e.g., "v2.3" -> "2.3")
                            string version = tagName.TrimStart('v', 'V');
                            
                            _logger.Info($"Version erfolgreich von GitHub API extrahiert: '{version}' (original tag: '{tagName}')");
                            return version;
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.StatusCode.HasValue)
                {
                    _logger.Error($"HTTP-Fehler beim Abrufen der neuesten Version von GitHub: Status {httpEx.StatusCode.Value} - {httpEx.Message}", httpEx);
                }
                else
                {
                    _logger.Error($"HTTP-Fehler beim Abrufen der neuesten Version von GitHub: {httpEx.Message}", httpEx);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.Error($"JSON-Parsing-Fehler beim Verarbeiten der GitHub API Antwort: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Abrufen der neuesten Version von GitHub: {ex.Message}", ex);
            }
            
            return null;
        }

        public async Task<NexusModFilesResponse?> GetModFilesAsync(string gameDomain, int modNumber, string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                string filesUrl = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modNumber}/files.json";
                string filesJson = await client.GetStringAsync(filesUrl, cancellationToken);
                return JsonSerializer.Deserialize<NexusModFilesResponse>(filesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Abrufen der Mod-Dateien für Mod {modNumber}", ex);
                return null;
            }
        }

        public async Task<string?> GetDownloadLinkAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                string downloadLinkEndpoint = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modNumber}/files/{fileId}/download_link.json";

                HttpResponseMessage responseMessage = await client.GetAsync(downloadLinkEndpoint, cancellationToken);

                if (responseMessage.StatusCode == HttpStatusCode.Forbidden)
                {
                    return null; // Nicht Premium
                }

                responseMessage.EnsureSuccessStatusCode();
                string downloadJson = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                string? downloadLink = null;

                using (JsonDocument doc = JsonDocument.Parse(downloadJson))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in doc.RootElement.EnumerateArray())
                        {
                            if (element.TryGetProperty("URI", out JsonElement uriElement) && uriElement.ValueKind == JsonValueKind.String)
                            {
                                downloadLink = uriElement.GetString();
                                break;
                            }
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("URI", out JsonElement uriElement) && uriElement.ValueKind == JsonValueKind.String)
                        {
                            downloadLink = uriElement.GetString();
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        downloadLink = doc.RootElement.GetString();
                    }
                }

                return downloadLink;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Abrufen des Download-Links für Mod {modNumber}, File {fileId}", ex);
                return null;
            }
        }

        public async Task<byte[]> DownloadFileAsync(string downloadUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await client.GetByteArrayAsync(downloadUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Herunterladen der Datei von {downloadUrl}", ex);
                throw;
            }
        }

        /// <summary>
        /// Führt ein Premium-Update für einen Mod durch (Download)
        /// WICHTIG: Nur für Premium-Benutzer - gibt null zurück, wenn nicht Premium oder Fehler
        /// Gibt den Pfad zur temporären Datei zurück
        /// </summary>
        public async Task<string?> PerformPremiumUpdateAsync(string gameDomain, int modNumber, int fileId, string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                // Prüfe Premium-Status
                if (!_settings.NexusIsPremium)
                {
                    _logger.Warning($"Benutzer ist nicht Premium - Update für Mod {modNumber} nicht möglich");
                    return null;
                }

                // Hole Download-Link
                string? downloadLink = await GetDownloadLinkAsync(gameDomain, modNumber, fileId, apiKey, cancellationToken);
                if (string.IsNullOrEmpty(downloadLink))
                {
                    _logger.Error($"Download-Link für Mod {modNumber}, File {fileId} konnte nicht abgerufen werden");
                    return null;
                }

                // Lade Datei herunter
                _logger.Info($"Lade Update für Mod {modNumber} herunter...");
                byte[] fileData = await DownloadFileAsync(downloadLink, cancellationToken);

                // Speichere temporäre Datei
                string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mod_update_{modNumber}_{Guid.NewGuid()}.zip");
                await System.IO.File.WriteAllBytesAsync(tempFilePath, fileData, cancellationToken);

                _logger.Info($"Update-Datei für Mod {modNumber} erfolgreich heruntergeladen: {tempFilePath}");
                
                // Rückgabe der temporären Datei - wird vom ViewModel verwendet
                return tempFilePath;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Premium-Update für Mod {modNumber}: {ex.Message}", ex);
                return null;
            }
        }

        public bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(_settings.NexusUserToken);
        }
    }
}

