using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Net;



namespace KCD2_mod_manager
{
    public partial class MainWindow : Window
    {
        private const string DefaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\KingdomComeDeliverance2\Bin\Win64MasterMasterSteamPGO\KingdomCome.exe";
        private string GamePath;
        private string ModFolder;
        private Point _dragStartPoint;

        // Neue Felder:
        private bool isDarkMode = false;
        private bool sortByLoadOrder = true; // true: sortiert nach load order (Drag&Drop aktiv), false: sortiert nach Titel
        private bool? savedSortByLoadOrder = null;
        private string additionalLaunchArgs = Settings.Default.GameLaunchArgs;

        private ObservableCollection<Mod> Mods = new ObservableCollection<Mod>();

        private ICollectionView ModsView;

        private FileSystemWatcher modFolderWatcher;


        private const string ModNotesFileName = "mod_notes.json";
        private Dictionary<string, string> modNotes = new Dictionary<string, string>();

        private const string currentManagerVersion = "1.9";

        public MainWindow()
        {


            InitializeComponent();

            isDarkMode = Settings.Default.IsDarkMode;
            UpdateTheme();


            CheckAndLoadGamePath();
            if (Settings.Default.BackupOnStartup)
            {
                CreateModsBackup();
            }



            //InitializeModFolderWatcher();
            LoadMods();
            ModsView = CollectionViewSource.GetDefaultView(Mods);
            ModList.ItemsSource = Mods;

            // Enable Drag-and-Drop
            this.AllowDrop = true;
            this.DragOver += Window_DragOver;
            this.Drop += Window_Drop;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            CheckNexusLoginStatusAsync();

            CheckForUpdateAsync();
            CheckForModUpdatesAsync();

        }
        private async Task CheckNexusLoginStatusAsync()
        {
            if (!IsUserLoggedInToNexus() && !Settings.Default.DontAskForNexusLogin)
            {
                var result = MessageBox.Show(
                    "You are not logged in to Nexus Mods. Do you want to log in now?\n\n(Click 'Cancel' to not be prompted again.)",
                    "Nexus Login Required",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await StartNexusSSOAsync();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    Settings.Default.DontAskForNexusLogin = true;
                    Settings.Default.Save();
                }
            }

            // Update status label
            if (IsUserLoggedInToNexus())
            {
                StatusLabel.Content = $"Logged in as {Settings.Default.NexusUsername}";
            }
            else
            {
                StatusLabel.Content = "Not logged in to Nexus Mods";
            }
        }

        private bool IsUserLoggedInToNexus()
        {
            return !string.IsNullOrEmpty(Settings.Default.NexusUserToken);
        }

        private async Task StartNexusSSOAsync()
        {
            // Generate a new UUID (GUID) for the SSO request
            string uuid = Guid.NewGuid().ToString();
            // Settings.Default.NexusSSO_UUID = uuid; Settings.Default.Save();

            // Define your application slug (must be assigned by Nexus Mods staff) and redirect URI.
            string applicationSlug = "kcd2modmanager"; // Replace with your app slug.
            string redirectUri = "http://127.0.0.1:12345/ssocallback/"; // Must be registered in your Nexus SSO settings.

            // Construct the SSO URL (this example includes a redirect URI parameter)
            string ssoUrl = $"https://www.nexusmods.com/sso?id={uuid}&application={applicationSlug}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

            // Open the SSO URL in the user's default browser
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ssoUrl,
                UseShellExecute = true
            });

            // Start a local HTTP listener to catch the redirect with the API key
            await StartLocalHttpListenerAsync(redirectUri);
        }

        private async Task StartLocalHttpListenerAsync(string redirectUri)
        {
            // Create an HttpListener with the prefix from redirectUri.
            var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            try
            {
                listener.Start();

                // Wait for an incoming request
                var context = await listener.GetContextAsync();
                var request = context.Request;

                // Parse the query string for the API key and username
                string apiKey = request.QueryString["api_key"];
                string username = request.QueryString["username"];

                // Save these values in settings (or use them as needed)
                if (!string.IsNullOrEmpty(apiKey))
                {
                    Settings.Default.NexusUserToken = apiKey;
                    Settings.Default.NexusUsername = username ?? "";
                    Settings.Default.Save();
                }

                // Send a simple response back to the browser
                var response = context.Response;
                string responseHtml = "<html><body>You are now logged in. You may close this window.</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during Nexus SSO login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                listener.Stop();
            }
        }
        private void LoginToNexus_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new NexusSSOLoginWindow();
            loginWindow.Owner = this;
            loginWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (loginWindow.ShowDialog() == true)
            {
                Settings.Default.NexusUserToken = loginWindow.Token;
                Settings.Default.NexusUsername = loginWindow.Username;
                Settings.Default.Save();
                StatusLabel.Content = $"Logged in as {Settings.Default.NexusUsername}";
            }
        }


        private async void CheckForUpdateAsync()
        {
            if (!Settings.Default.EnableUpdateNotifications)
                return;

            try
            {
                using (var client = new HttpClient())
                {
                    string url = "https://www.nexusmods.com/kingdomcomedeliverance2/mods/187";
                    string html = await client.GetStringAsync(url);
                    // Extract version using regex from meta tag
                    var match = Regex.Match(html, "<meta property=\"twitter:data1\" content=\"([^\"]+)\"");
                    if (match.Success)
                    {
                        string latestVersion = match.Groups[1].Value.Trim();
                        if (Version.TryParse(currentManagerVersion, out Version currentVersion) &&
                            Version.TryParse(latestVersion, out Version onlineVersion))
                        {
                            if (onlineVersion > currentVersion)
                            {
                                var result = MessageBox.Show($"A new version ({latestVersion}) is available. Do you want to update?",
                                    "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                                if (result == MessageBoxResult.Yes)
                                {
                                    System.Diagnostics.Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "https://www.nexusmods.com/kingdomcomedeliverance2/mods/187",
                                        UseShellExecute = true
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Optionally log errors. Do not block startup.
            }
        }

        private async void CheckForModUpdatesAsync()
        {
            string jsonPath = Path.Combine(ModFolder, "mod_versions.json");
            if (!File.Exists(jsonPath)) return;

            string json = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.WriteLine("JSON file is empty.");
                return;
            }

            // Deserialize into Dictionary<string, ModVersionInfo>
            var modData = JsonSerializer.Deserialize<Dictionary<string, ModVersionInfo>>(json);
            if (modData == null)
            {
                Debug.WriteLine("Error: modData is null.");
                return;
            }

            Debug.WriteLine($"Deserialized modData count: {modData.Count}");

            using (var client = new HttpClient())
            {
                // For each mod stored in the JSON
                foreach (var modEntry in modData)
                {
                    string modId = modEntry.Key;
                    int modNumber = modEntry.Value.ModNumber;  // the mod number stored during installation

                    // Debug output
                    Debug.WriteLine($"Processing mod: {modId}, ModNumber: {modNumber}");

                    // Get the corresponding mod object from the Mods collection
                    var modItem = Mods.FirstOrDefault(m => m.Id.Equals(modId, StringComparison.OrdinalIgnoreCase));
                    if (modItem != null)
                    {
                        modItem.ModNumber = modNumber;  // update if necessary

                        string installedVersion = modItem.Version; // installed version
                        string url = $"https://www.nexusmods.com/kingdomcomedeliverance2/mods/{modNumber}";

                        try
                        {
                            string html = await client.GetStringAsync(url);
                            var match = Regex.Match(html, "<meta property=\"twitter:data1\" content=\"([^\"]+)\"");
                            if (match.Success)
                            {
                                string latestVersion = match.Groups[1].Value.Trim();
                                if (Version.TryParse(installedVersion, out Version currentVersion) &&
                                    Version.TryParse(latestVersion, out Version onlineVersion) &&
                                    onlineVersion > currentVersion)
                                {
                                    modItem.HasUpdate = true;
                                    modItem.LatestVersion = latestVersion;
                                    ModList.Items.Refresh();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error checking update for mod {modId}: {ex.Message}");
                        }
                    }
                }
            }
        }





        private void SaveModVersion(string modId, string version, int modNumber, string installedFileName)
        {
            string jsonPath = Path.Combine(ModFolder, "mod_versions.json");
            Dictionary<string, ModVersionInfo> modData;

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                // Deserialisieren in das Dictionary mit dem eigenen Typ
                modData = JsonSerializer.Deserialize<Dictionary<string, ModVersionInfo>>(json, serializerOptions);
            }
            else
            {
                modData = new Dictionary<string, ModVersionInfo>();
            }

            modData[modId] = new ModVersionInfo
            {
                Version = version,
                ModNumber = modNumber,
                FileName = installedFileName
            };

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(modData, serializerOptions));
        }


        public class ModVersionInfo
        {
            public string Version { get; set; }
            public int ModNumber { get; set; }
            public string FileName { get; set; }
        }


        private string GetStoredFileName(string modId)
        {
            string jsonPath = Path.Combine(ModFolder, "mod_versions.json");
            if (!File.Exists(jsonPath)) return null;
            string json = File.ReadAllText(jsonPath);
            var modData = JsonSerializer.Deserialize<Dictionary<string, (string Version, int ModNumber, string FileName)>>(json);
            if (modData != null && modData.ContainsKey(modId))
            {
                return modData[modId].FileName;
            }
            return null;
        }

        private string ExtractVersionFromFileName(string fileName)
        {
            // This is a simple example; adjust the regex as needed.
            // It assumes the version is the third hyphen-separated group.
            var parts = fileName.Split('-');
            if (parts.Length >= 4)
            {
                return parts[2]; // e.g., "1" in "KCD2 Mod Manager-187-1-0-1738943255.zip"
            }
            return "0";
        }





        private void OpenModPage_Click(object sender, RoutedEventArgs e)
        {
            Mod mod = null;

            if (sender is Button btn && btn.DataContext is Mod modBtn)
            {
                mod = modBtn;
            }
            else if (sender is MenuItem mi && mi.DataContext is Mod modMenu)
            {
                mod = modMenu;
            }

            if (mod != null && mod.ModNumber > 0)
            {
                string url = $"https://www.nexusmods.com/kingdomcomedeliverance2/mods/{mod.ModNumber}";
                System.Diagnostics.Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Mod number is missing or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private static int CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0;

            source = source.ToLower();
            target = target.ToLower();

            int[,] dp = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                for (int j = 0; j <= target.Length; j++)
                {
                    if (i == 0) dp[i, j] = j;
                    else if (j == 0) dp[i, j] = i;
                    else
                    {
                        int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                        dp[i, j] = Math.Min(Math.Min(
                            dp[i - 1, j] + 1,
                            dp[i, j - 1] + 1),
                            dp[i - 1, j - 1] + cost);
                    }
                }
            return dp[source.Length, target.Length];
        }


        private async void UpdateMod_Click(object sender, RoutedEventArgs e)
        {
            // Determine which mod is being updated (Button or MenuItem)
            Mod mod = null;
            if (sender is Button btn && btn.DataContext is Mod modBtn)
            {
                mod = modBtn;
            }
            else if (sender is MenuItem mi && mi.DataContext is Mod modMenu)
            {
                mod = modMenu;
            }

            if (mod == null)
            {
                MessageBox.Show("No mod selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (mod.ModNumber <= 0)
            {
                MessageBox.Show("Mod number is missing or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save current mod state (enabled and list position)
            bool wasEnabled = mod.IsEnabled;
            int oldIndex = Mods.IndexOf(mod);

            string gameDomain = "kingdomcomedeliverance2";
            int modNumber = mod.ModNumber;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", Settings.Default.NexusUserToken);
                string filesUrl = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modNumber}/files.json";
                try
                {
                    string filesJson = await client.GetStringAsync(filesUrl);
                    var response = JsonSerializer.Deserialize<NexusModFilesResponse>(filesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (response?.files == null || response.files.Count == 0)
                    {
                        MessageBox.Show("No files found for this mod.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string storedFileName = GetStoredFileName(mod.Id);

                    //update chain
                    NexusModFileUpdate finalUpdate = null;
                    string currentFileName = storedFileName;
                    if (!string.IsNullOrEmpty(currentFileName) && response.file_updates != null && response.file_updates.Count > 0)
                    {
                        bool foundUpdate = true;
                        while (foundUpdate)
                        {
                            foundUpdate = false;
                            foreach (var update in response.file_updates)
                            {
                                if (update.old_file_name.Equals(currentFileName, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Update gefunden – setze currentFileName auf den neuen Namen und speichere finalUpdate
                                    currentFileName = update.new_file_name;
                                    finalUpdate = update;
                                    foundUpdate = true;
                                    break;
                                }
                            }
                        }
                    }

                    NexusModFile fileToUse = null;
                    if (finalUpdate != null)
                    {
                        // Update-Kette gefunden: Versuche, die Datei mit der finalen new_file_id aus response.files zu ermitteln
                        fileToUse = response.files.FirstOrDefault(f => f.file_id == finalUpdate.new_file_id);
                        if (fileToUse == null)
                        {
                            // Falls nicht gefunden, erstelle ein Dummy-Objekt (Version wird aus dem Dateinamen extrahiert)
                            fileToUse = new NexusModFile
                            {
                                file_id = finalUpdate.new_file_id,
                                version = ExtractVersionFromFileName(finalUpdate.new_file_name),
                                uploaded_timestamp = finalUpdate.uploaded_timestamp
                            };
                        }
                    }
                    else
                    {
                        // Get all possible update files sorted by uploaded timestamp (newest first)
                        var possibleUpdates = response.files.OrderByDescending(f => f.uploaded_timestamp).ToList();

                        // Filter out only those files that have a valid mod_version.
                        var filesWithVersion = possibleUpdates.Where(f =>
                        {
                            Version ver;
                            return Version.TryParse(f.mod_version, out ver);
                        }).ToList();


                        // NexusModFile fileToUse = null;
                        if (filesWithVersion.Count > 0)
                        {
                            // Find the highest mod_version
                            Version highestModVersion = filesWithVersion.Max(f =>
                            {
                                Version v;
                                Version.TryParse(f.mod_version, out v);
                                return v;
                            });

                            // Get candidates that have that highest mod_version.
                            var candidateUpdates = filesWithVersion.Where(f =>
                            {
                                Version v;
                                Version.TryParse(f.mod_version, out v);
                                return v.Equals(highestModVersion);
                            }).ToList();

                            if (candidateUpdates.Count == 1)
                            {
                                fileToUse = candidateUpdates.First();
                            }
                            else if (candidateUpdates.Count > 1)
                            {
                                // Try to determine the best match based on the mod name similarity.
                                var bestMatch = candidateUpdates
                                    .OrderByDescending(f => CalculateSimilarity(f.name, mod.Name))
                                    .FirstOrDefault();

                                // Build a selection string for the user.
                                string options = string.Join("\n", candidateUpdates.Select((f, i) =>
                                    $"{i + 1}: {f.name} (Version: {f.mod_version})"));
                                string input = Microsoft.VisualBasic.Interaction.InputBox(
                                    $"There are multiple candidate updates. Please choose one:\n\n{options}",
                                    "Select Update",
                                    "1");

                                if (int.TryParse(input, out int choice) && choice > 0 && choice <= candidateUpdates.Count)
                                {
                                    fileToUse = candidateUpdates[choice - 1];
                                }
                                else
                                {
                                    MessageBox.Show("Invalid input. Updated Canceld", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                    //fileToUse = bestMatch ?? candidateUpdates.First();
                                    return;
                                }
                            }
                            else
                            {
                                // Fallback if no candidate with valid mod_version is found.
                                fileToUse = possibleUpdates.OrderByDescending(f => f.uploaded_timestamp).FirstOrDefault();
                            }
                        }
                        else
                        {
                            // Fallback if none of the files have a valid mod_version.
                            fileToUse = possibleUpdates.OrderByDescending(f => f.uploaded_timestamp).FirstOrDefault();
                        }
                    }


                    if (fileToUse == null)
                    {
                        MessageBox.Show("Could not determine a current file for this mod.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Compare installed version with online version
                    if (Version.TryParse(mod.Version, out Version installedVersion) &&
                        Version.TryParse(fileToUse.version, out Version onlineVersion))
                    {
                        if (onlineVersion <= installedVersion)
                        {
                            MessageBox.Show("The mod is already up to date.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }

                    // Generate download link for the chosen file using the Nexus Mods API
                    string downloadLinkEndpoint = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modNumber}/files/{fileToUse.file_id}/download_link.json";
                    string downloadJson = await client.GetStringAsync(downloadLinkEndpoint);
                    string downloadLink = null;
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
                        else
                        {
                            MessageBox.Show("Unexpected response from update API.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (string.IsNullOrEmpty(downloadLink))
                    {
                        MessageBox.Show("Error retrieving the download link.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Download the update file to a temporary location
                    string tempDownloadPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(new Uri(downloadLink).LocalPath));
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadLink);
                    File.WriteAllBytes(tempDownloadPath, fileBytes);

                    // Remove the mod from the list before updating
                    Mods.RemoveAt(oldIndex);

                    // Install update by calling ProcessModUpdate:
                    // We assume ProcessModUpdate extracts the update file, flattens the structure, and forces the original ID
                    ProcessModUpdate(tempDownloadPath, mod.Path, mod.Id);

                    // Create updated mod entry (re-read manifest from mod.Path)
                    var manifestData = ParseManifest(Path.Combine(mod.Path, "mod.manifest"));
                    var updatedMod = new Mod
                    {
                        Id = mod.Id,  // original id forced in manifest by ProcessModUpdate
                        Name = manifestData.Item2,
                        Version = manifestData.Item3,
                        Path = mod.Path,
                        ModNumber = mod.ModNumber,
                        IsEnabled = wasEnabled
                    };

                    // Reinsert at the original index
                    Mods.Insert(oldIndex, updatedMod);
                    SaveModOrder();
                    RefreshAlternationIndexes();
                    ModList.Items.Refresh();

                    // Delete the downloaded file
                    if (File.Exists(tempDownloadPath))
                        File.Delete(tempDownloadPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating the mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private Mod ProcessModUpdate(string archivePath, string targetDir, string originalId)
        {
            // 1. Clear target directory
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            Directory.CreateDirectory(targetDir);

            // 2. Extract the archive into the target directory
            using (var archive = ArchiveFactory.Open(archivePath))
            {
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    entry.WriteToDirectory(targetDir, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

            // 3. Locate the mod.manifest file
            string manifestPath = Directory.EnumerateFiles(targetDir, "mod.manifest", SearchOption.AllDirectories)
                                           .FirstOrDefault();

            // 4. If no manifest is found, generate one
            if (manifestPath == null)
            {
                manifestPath = GenerateManifest(targetDir);
            }
            else
            {
                // 5. If the manifest is in a nested folder, move its contents up
                string manifestDir = Path.GetDirectoryName(manifestPath);
                if (!string.Equals(manifestDir.TrimEnd(Path.DirectorySeparatorChar), targetDir.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    // Move all subdirectories and files from manifestDir up to targetDir
                    foreach (var dir in Directory.GetDirectories(manifestDir))
                    {
                        string destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                        Directory.Move(dir, destDir);
                    }
                    foreach (var file in Directory.GetFiles(manifestDir))
                    {
                        string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                        File.Move(file, destFile);
                    }
                    // Optionally, delete the now-empty nested directory
                    if (Directory.GetFiles(manifestDir).Length == 0 && Directory.GetDirectories(manifestDir).Length == 0)
                    {
                        Directory.Delete(manifestDir);
                    }
                    // Update manifestPath to point to the new location in targetDir
                    manifestPath = Path.Combine(targetDir, "mod.manifest");
                }
            }

            // 6. Parse the manifest data (which should now be at targetDir/mod.manifest)
            var manifestInfo = ParseManifest(manifestPath);
            if (manifestInfo == null)
            {
                throw new Exception("Failed to parse manifest after update.");
            }

            // 7. Return a new Mod object using the originalId (forcing the mod id to remain unchanged).
            return new Mod
            {
                Id = originalId,  // Force original ID to remain unchanged
                Name = manifestInfo.Item2,
                Version = manifestInfo.Item3,
                Path = targetDir,
                ModNumber = -1 // Temporary value – should be set elsewhere if needed
            };
        }


        // Hilfsfunktion zur Extraktion des Download-Links
        private string ExtractDownloadLink(string jsonResponse)
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement element in doc.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("URI", out JsonElement uriElement) && uriElement.ValueKind == JsonValueKind.String)
                        {
                            return uriElement.GetString();
                        }
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("URI", out JsonElement uriElement) && uriElement.ValueKind == JsonValueKind.String)
                    {
                        return uriElement.GetString();
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.String)
                {
                    return doc.RootElement.GetString();
                }
            }
            return null;
        }


        private async void EndorseMod_Click(object sender, RoutedEventArgs e)
        {
            // Determine which mod is being endorsed (from a Button or MenuItem)
            Mod mod = null;
            if (sender is Button btn && btn.DataContext is Mod modBtn)
            {
                mod = modBtn;
            }
            else if (sender is MenuItem mi && mi.DataContext is Mod modMenu)
            {
                mod = modMenu;
            }

            if (mod == null)
            {
                MessageBox.Show("No mod selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure the mod number is valid
            if (mod.ModNumber <= 0)
            {
                MessageBox.Show("Mod number is missing or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Define the game domain name (adjust if needed)
            string gameDomain = "kingdomcomedeliverance2";
            int modId = mod.ModNumber; // use mod.ModNumber as the numeric mod id

            using (HttpClient client = new HttpClient())
            {
                // Set the API key header (ensure your API key is valid and stored in settings)
                client.DefaultRequestHeaders.Add("apikey", Settings.Default.NexusUserToken);

                // Build the endorsement URL
                string endorseUrl = $"https://api.nexusmods.com/v1/games/{gameDomain}/mods/{modId}/endorse.json";

                try
                {
                    // Send a POST request with no content (the API expects only the header)
                    HttpResponseMessage response = await client.PostAsync(endorseUrl, null);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Mod endorsed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Endorse failed: {response.ReasonPhrase}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error endorsing mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void ToggleUpdateNotifications_Click(object sender, RoutedEventArgs e)
        {
            // Umschalten der Update-Benachrichtigungen
            Settings.Default.EnableUpdateNotifications = !Settings.Default.EnableUpdateNotifications;
            Settings.Default.Save();

            string status = Settings.Default.EnableUpdateNotifications ? "enabled" : "disabled";
            MessageBox.Show("Update notifications are now " + status + ".", "Update Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Speichern nur im Normalzustand
            if (this.WindowState == WindowState.Normal)
            {
                Settings.Default.WindowWidth = (int)this.Width;
                Settings.Default.WindowHeight = (int)this.Height;
                Settings.Default.WindowLeft = (int)this.Left;
                Settings.Default.WindowTop = (int)this.Top;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                // Speichern bei maximiertem Zustand
                Settings.Default.WindowWidth = (int)this.RestoreBounds.Width;
                Settings.Default.WindowHeight = (int)this.RestoreBounds.Height;
                Settings.Default.WindowLeft = (int)this.RestoreBounds.Left;
                Settings.Default.WindowTop = (int)this.RestoreBounds.Top;
            }

            // Fensterzustand immer speichern (Normal, Maximiert, Minimiert)
            Settings.Default.WindowState = this.WindowState.ToString();

            // Änderungen in den Settings speichern
            Settings.Default.Save();
        }

        private void ToggleBackupCreation_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.CreateBackup = !Settings.Default.CreateBackup;
            Settings.Default.Save();
            MessageBox.Show($"Backup Creation is now {(Settings.Default.CreateBackup ? "Enabled" : "Disabled")}.",
                            "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleBackupOnStartup_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BackupOnStartup = !Settings.Default.BackupOnStartup;
            Settings.Default.Save();
            MessageBox.Show($"Backup on Startup is now {(Settings.Default.BackupOnStartup ? "Enabled" : "Disabled")}.",
                            "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleBackupOnChange_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.BackupOnChange = !Settings.Default.BackupOnChange;
            Settings.Default.Save();
            MessageBox.Show($"Backup on Mod Folder Change is now {(Settings.Default.BackupOnChange ? "Enabled" : "Disabled")}.",
                            "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetMaxBackups_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter the maximum number of backups to keep:",
                                                                       "Set Max Backups",
                                                                       Settings.Default.BackupMaxCount.ToString());

            if (int.TryParse(input, out int maxBackups) && maxBackups > 0)
            {
                Settings.Default.BackupMaxCount = maxBackups;
                Settings.Default.Save();
                MessageBox.Show($"Max Backups set to {maxBackups}.", "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Invalid number. Please enter a positive integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateModsBackup()
        {
            if (!Settings.Default.CreateBackup)
                return;

            try
            {
                string parentDir = Path.GetDirectoryName(ModFolder);
                string backupRoot = Path.Combine(parentDir, "Mods_Backup");

                // Sicherstellen, dass der Backup-Ordner existiert
                if (!Directory.Exists(backupRoot))
                {
                    Directory.CreateDirectory(backupRoot);
                }

                // Neuen Backup-Ordner mit Timestamp erstellen
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFolder = Path.Combine(backupRoot, $"Mods_Backup_{timestamp}");
                Directory.CreateDirectory(backupFolder);

                // Mods-Ordner in Backup kopieren
                CopyDirectory(ModFolder, backupFolder);

                // Alte Backups bereinigen, falls mehr als MaxCount existieren
                int maxBackups = Settings.Default.BackupMaxCount;
                var backupFolders = new DirectoryInfo(backupRoot).GetDirectories()
                                    .OrderByDescending(d => d.CreationTime)
                                    .ToList();

                if (backupFolders.Count > maxBackups)
                {
                    foreach (var oldBackup in backupFolders.Skip(maxBackups))
                    {
                        oldBackup.Delete(true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create mods backup: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Größe und Position setzen
            this.Width = Settings.Default.WindowWidth;
            this.Height = Settings.Default.WindowHeight;
            this.Left = Settings.Default.WindowLeft;
            this.Top = Settings.Default.WindowTop;

            // Fensterzustand (Minimiert, Maximiert, Normal)
            if (Enum.TryParse(Settings.Default.WindowState, out WindowState state))
            {
                this.WindowState = state;
            }

            // Wenn das Fenster maximiert ist, dann sicherstellen, dass es die maximalisierte Größe hat
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
            }

            // Verhindere, dass das Fenster außerhalb des sichtbaren Bildschirms startet
            EnsureWindowIsVisible();
        }



        private void EnsureWindowIsVisible()
        {
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight) this.Top = screenHeight - this.Height;
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;
        }


        protected override void OnClosed(EventArgs e)
        {
           modFolderWatcher?.Dispose();
           SaveModOrder();
            base.OnClosed(e);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchTextBox.Text.ToLowerInvariant();
            // If there's any search term, disable sort (if not already disabled)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (savedSortByLoadOrder == null)
                {
                    // Save current sort mode
                    savedSortByLoadOrder = sortByLoadOrder;
                }
                // Disable sorting while searching
                sortByLoadOrder = false;
                SortButton.Content = "Sort: Disabled";
            }
            else
            {
                // When the search box is cleared, restore the saved sort mode if available
                if (savedSortByLoadOrder != null)
                {
                    sortByLoadOrder = savedSortByLoadOrder.Value;
                    savedSortByLoadOrder = null;
                    // Optionally, update the sort button text based on the restored state
                    SortButton.Content = sortByLoadOrder ? "Sort: Load Order" : "Sort: Title";
                }
            }

            // Apply the search filter to the collection view
            ModsView.Filter = (item) =>
            {
                if (item is Mod mod)
                {
                    return mod.Name.ToLowerInvariant().Contains(filter) ||
                           mod.Version.ToLowerInvariant().Contains(filter);
                }
                return false;
            };
            ModsView.Refresh();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
        }


        // --- Sort Toggle ---
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                MessageBox.Show("Sorting is disabled during search.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            sortByLoadOrder = !sortByLoadOrder;
            if (!sortByLoadOrder)
            {
                var sorted = Mods.OrderBy(m => m.Name).ToList();
                Mods.Clear();
                foreach (var mod in sorted)
                    Mods.Add(mod);
                SortButton.Content = "Sort: Title";
            }
            else
            {
                LoadMods();
                SortButton.Content = "Sort: Load Order";
            }
        }



        //private void InitializeModFolderWatcher()
        //{
        //    modFolderWatcher = new FileSystemWatcher(ModFolder)
        //    {
        //        NotifyFilter = NotifyFilters.DirectoryName, // Nur Änderungen an Ordnern überwachen
        //        Filter = "*.*", // Alle Dateien und Ordner überwachen
        //        IncludeSubdirectories = false, // Nur den Mod-Ordner selbst überwachen
        //        EnableRaisingEvents = true // Überwachung aktivieren
        //    };

        //    modFolderWatcher.Created += OnModFolderChanged;
        //    modFolderWatcher.Deleted += OnModFolderChanged;
        //}

        //private void OnModFolderChanged(object sender, FileSystemEventArgs e)
        //{
        //    // Überprüfe, ob ein neuer Mod-Ordner erstellt wurde
        //    if (Directory.Exists(e.FullPath) && File.Exists(Path.Combine(e.FullPath, "mod.manifest")))
        //    {
        //        // Mod neu laden
        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            LoadMods(); // Mods neu laden
        //            SaveModOrder();
        //            MessageBox.Show($"New mod detected: {e.Name}. Reloaded mods.", "Mod Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        //        });
        //    }
        //}
        private void CheckAndLoadGamePath()
        {
            // Lade den GamePath aus den Einstellungen
            GamePath = Settings.Default.GamePath;

            if (string.IsNullOrWhiteSpace(GamePath))
            {
                // Prüfe den Standardpfad
                if (File.Exists(DefaultGamePath))
                {
                    GamePath = DefaultGamePath;
                    Settings.Default.GamePath = GamePath;
                    Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("The game was not found in the default path. Please select the game executable manually.", "Game Path Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "Game Executable (*.exe)|*.exe",
                        Title = "Select Kingdom Come Deliverance 2 Executable"
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        GamePath = openFileDialog.FileName;
                        Settings.Default.GamePath = GamePath;
                        Settings.Default.Save();
                    }
                    else
                    {
                        MessageBox.Show("The game path is required to continue. Exiting the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
            }

            if (!File.Exists(GamePath))
            {
                MessageBox.Show("The saved game path is invalid. Please update the path in settings.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Bestimme den Mods-Ordner (eine Ebene oberhalb der Spielinstallation, anpassen wie benötigt)
            ModFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(GamePath))), "Mods");
        }




        private void SaveGamePath()
        {
            Settings.Default.GamePath = GamePath;
            Settings.Default.Save();
            ModFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(GamePath))), "Mods");
            LoadMods();
        }


        private void LoadMods()
        {
            // Sicherstellen, dass das Mod-Verzeichnis existiert
            if (!Directory.Exists(ModFolder))
                Directory.CreateDirectory(ModFolder);

            Mods.Clear();

            // Lese mod_order.txt zeilenweise und interpretiere sie als (modId, isEnabled)
            var modOrderPath = Path.Combine(ModFolder, "mod_order.txt");
            var modOrderList = new List<(string modId, bool isEnabled)>();
            if (File.Exists(modOrderPath))
            {
                var lines = File.ReadAllLines(modOrderPath)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .ToList();
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    bool isEnabled = true;
                    if (trimmed.StartsWith("#"))
                    {
                        isEnabled = false;
                        // Entferne das führende "#" und zusätzliche Leerzeichen
                        trimmed = trimmed.Substring(1).Trim();
                    }
                    modOrderList.Add((trimmed, isEnabled));
                }
            }

            // Alle Mod-Ordner im ModFolder finden: entweder mit einer mod.manifest oder mit .pak-Dateien
            var allMods = Directory.GetDirectories(ModFolder)
                                   .Where(dir =>
                                       File.Exists(Path.Combine(dir, "mod.manifest")) ||
                                       Directory.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories).Any());

            // Erstelle ein Dictionary (Key: modId) zur schnellen Zuordnung
            var modDictionary = new Dictionary<string, Mod>();
            foreach (var dir in allMods)
            {
                string manifestPath = Path.Combine(dir, "mod.manifest");
                // Falls keine manifest existiert, aber .pak-Dateien vorhanden sind, generiere ein Manifest
                if (!File.Exists(manifestPath))
                {
                    bool hasPakFiles = Directory.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = GenerateManifest(dir);
                    }
                    else
                    {
                        // Überspringe diesen Ordner, wenn weder manifest noch .pak-Dateien vorhanden sind
                        continue;
                    }
                }

                // Parse das Manifest (welches auch ggf. automatisch die modid generiert und speichert)
                var modInfo = ParseManifest(manifestPath);
                if (modInfo != null)
                {
                    var modId = modInfo.Item1;    // Erwartete modid, z. B. "midnightandblackknightarmorb"
                    var modName = modInfo.Item2;  // Beispiel: "Midnight and Black Knight Armor B 1243AGDfs @"
                    var modVersion = modInfo.Item3;

                    // Überprüfe, ob der aktuelle Ordnername (ohne Pfad) der erwarteten modid entspricht.
                    // Überprüfe, ob der Ordnername der ID entspricht, und korrigiere ihn gegebenenfalls
                    var expectedPath = Path.Combine(ModFolder, modId);
                    if (!dir.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (Directory.Exists(expectedPath))
                            {
                                Directory.Delete(expectedPath, true);
                            }
                            Directory.Move(dir, expectedPath);
                        }
                        catch (IOException ioEx)
                        {
                            // Fehlermeldung anzeigen und den vorhandenen Ordnerpfad beibehalten
                            MessageBox.Show($"Failed to move mod folder from {dir} to {expectedPath}:\n{ioEx.Message}\n\nPlease run the application as administrator or adjust folder permissions.",
                                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            expectedPath = dir; // Nutze den ursprünglichen Ordnerpfad
                        }
                    }


                    var mod = new Mod
                    {
                        Id = modId,
                        Name = modName,
                        Version = modVersion,
                        Path = expectedPath,
                        IsEnabled = true // Standardmäßig aktiviert, wird später ggf. überschrieben
                    };
                    modDictionary[modId] = mod;
                }
            }

            // Füge die Mods in der Reihenfolge aus der mod_order.txt hinzu (Load Order)
            int index = 1;
            foreach (var (modId, isEnabled) in modOrderList)
            {
                if (modDictionary.ContainsKey(modId))
                {
                    var mod = modDictionary[modId];
                    mod.Number = index++;
                    mod.IsEnabled = isEnabled; // Setze den Aktivierungsstatus gemäß mod_order.txt
                    Mods.Add(mod);
                    modDictionary.Remove(modId);
                }
            }

            // Füge alle übrigen Mods hinzu (die nicht in mod_order.txt gelistet sind)
            foreach (var remainingMod in modDictionary.Values)
            {
                remainingMod.Number = 0; // Keine Nummerierung, da nicht in der Reihenfolge enthalten
                Mods.Add(remainingMod);
            }

            UpdateModsEnabledCount();
            LoadModNotes();
        }


        private void LoadModNotes()
        {
            // Store the mod notes file in the ModFolder
            string notesPath = Path.Combine(ModFolder, ModNotesFileName);
            if (File.Exists(notesPath))
            {
                try
                {
                    string json = File.ReadAllText(notesPath);
                    modNotes = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }
                catch
                {
                    modNotes = new Dictionary<string, string>();
                }
            }
            else
            {
                modNotes = new Dictionary<string, string>();
            }
        }

        private void SaveModNotes()
        {
            string notesPath = Path.Combine(ModFolder, ModNotesFileName);
            string json = JsonSerializer.Serialize(modNotes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(notesPath, json);
        }


        private void UpdateModsEnabledCount()
        {
            int enabledCount = Mods.Count(mod => mod.IsEnabled);
            ModsEnabledCount.Text = $"Mods enabled: {enabledCount}";
        }


        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadMods();
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Mod Files (*.rar;*.7z;*.zip)|*.rar;*.7z;*.zip",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    ProcessModFile(file);
                }
            }
        }

        //private void AddModFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    using (var folderDialog = new FolderBrowserDialog())
        //    {
        //        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        {
        //            ProcessModFolder(folderDialog.SelectedPath);
        //        }
        //    }
        //}

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GamePath))
            {
                MessageBox.Show("The game executable path is invalid. Please update the path in settings before starting the game.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var enabledMods = Mods.Where(mod => mod.IsEnabled).ToList();
            if (!enabledMods.Any())
            {
                MessageBox.Show("No mods selected to load.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusLabel.Content = "Starting game with mods...";
            string extraArgs = string.IsNullOrWhiteSpace(additionalLaunchArgs) ? "" : additionalLaunchArgs + " ";

            // Wenn im Entwickler-Modus, füge das Argument -devmode hinzu
            if (Settings.Default.IsDevMode)
            {
                extraArgs += "-devmode ";
            }

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = GamePath,
                Arguments = extraArgs
            };

            try
            {
                System.Diagnostics.Process.Start(processInfo);
                StatusLabel.Content = "Game started.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start the game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusLabel.Content = "Failed to start game.";
            }
        }




        private void ProcessModFolder(string folderPath)
        {
            // Versuche, die mod.manifest-Datei im gesamten Ordner zu finden
            var manifestPath = Directory.EnumerateFiles(folderPath, "mod.manifest", SearchOption.AllDirectories).FirstOrDefault();

            // Falls keine manifest vorhanden ist, prüfe, ob .pak-Dateien vorhanden sind (z.B. in Data oder Localization)
            if (manifestPath == null)
            {
                bool hasPakFiles = Directory.EnumerateFiles(folderPath, "*.pak", SearchOption.AllDirectories).Any();
                if (hasPakFiles)
                {
                    // Generiere ein Manifest automatisch
                    manifestPath = GenerateManifest(folderPath);
                }
                else
                {
                    MessageBox.Show("This mod folder is not compatible (missing mod.manifest and no .pak files found).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


            string xmlContent = CorrectXmlVersionInFile(manifestPath);
            File.WriteAllText(manifestPath, xmlContent);

            XDocument manifestDoc = XDocument.Load(manifestPath);
            var infoElement = manifestDoc.Descendants("info").FirstOrDefault();
            if (infoElement != null)
            {
                var modidElement = infoElement.Element("modid");
                // Hier erzwingen wir, dass die modid immer vom Manager vergeben wird:
                var nameElement = infoElement.Element("name")?.Value?.Trim();
                if (!string.IsNullOrEmpty(nameElement))
                {
                    var generatedId = Regex.Replace(nameElement.ToLowerInvariant(), @"[^a-z]", "");
                    if (modidElement == null)
                    {
                        infoElement.Add(new XElement("modid", generatedId));
                    }
                    else
                    {
                        modidElement.Value = generatedId;
                    }
                    manifestDoc.Save(manifestPath);
                }
            }


            var manifestData = ParseManifest(manifestPath);
            if (manifestData == null || string.IsNullOrEmpty(manifestData.Item3))
            {
                MessageBox.Show("Invalid mod.manifest file. Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var modId = manifestData.Item1;
            var modName = manifestData.Item2;
            var modVersion = manifestData.Item3;
            var modTargetPath = Path.Combine(ModFolder, modId);

            if (Directory.Exists(modTargetPath))
            {
                // Optional: Versionsvergleich durchführen
                Directory.Delete(modTargetPath, true);
                LoadMods();
            }

            Directory.CreateDirectory(modTargetPath);
            CopyDirectory(folderPath, modTargetPath);
            Mods.Add(new Mod { Id = modId, Name = modName, Version = modVersion, Path = modTargetPath, IsEnabled = false });
            StatusLabel.Content = $"Added mod: {modName} (Version: {modVersion})";
        }


        private int ExtractModNumberFromFileName(string fileName)
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(fileName), "\\b(\\d+)\\b");
            return match.Success ? int.Parse(match.Value) : -1;
        }

        private void ProcessModFile(string filePath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(filePath));
            Directory.CreateDirectory(tempDir);

            int modNumber = ExtractModNumberFromFileName(filePath);
            if (modNumber == -1)
            {
                NameInputDialog dialog = new NameInputDialog("");
                dialog.Prompt = "Enter Mod Number:";
                dialog.Owner = this;
                dialog.Title = "Mod Number";
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    if (int.TryParse(dialog.EnteredText, out int enteredNumber))
                    {
                        modNumber = enteredNumber;
                    }
                    else
                    {
                        MessageBox.Show("Invalid number entered. Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Directory.Delete(tempDir, true);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("No number entered. Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Directory.Delete(tempDir, true);
                    return;
                }
            }

            try
            {
                ExtractArchive(filePath, tempDir);

                var manifestPath = Directory.EnumerateFiles(tempDir, "mod.manifest", SearchOption.AllDirectories).FirstOrDefault();
                if (manifestPath == null)
                {
                    bool hasPakFiles = Directory.EnumerateFiles(tempDir, "*.pak", SearchOption.AllDirectories).Any();
                    if (hasPakFiles)
                    {
                        manifestPath = GenerateManifest(tempDir);
                    }
                    else
                    {
                        MessageBox.Show("This mod is not compatible (missing mod.manifest and no .pak files found).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Directory.Delete(tempDir, true);
                        return;
                    }
                }

                string xmlContent = CorrectXmlVersionInFile(manifestPath);
                File.WriteAllText(manifestPath, xmlContent);

                var manifestData = ParseManifest(manifestPath);
                if (manifestData == null || string.IsNullOrEmpty(manifestData.Item3))
                {
                    var result = MessageBox.Show("The mod.manifest file is invalid. Do you want to attempt to generate a new manifest?",
                                                 "Invalid Manifest", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        manifestPath = GenerateManifest(tempDir);
                        manifestData = ParseManifest(manifestPath);
                        if (manifestData == null || string.IsNullOrEmpty(manifestData.Item3))
                        {
                            MessageBox.Show("Failed to generate a valid manifest. Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Directory.Delete(tempDir, true);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Directory.Delete(tempDir, true);
                        return;
                    }
                }

                var modId = manifestData.Item1;
                var modName = manifestData.Item2;
                var modVersion = manifestData.Item3;
                var modTargetPath = Path.Combine(ModFolder, modId);

                if (Directory.Exists(modTargetPath))
                {
                    var existingManifestPath = Path.Combine(modTargetPath, "mod.manifest");
                    var existingInfo = ParseManifest(existingManifestPath);
                    if (existingInfo != null && Version.TryParse(existingInfo.Item3, out var existingVersion) &&
                        Version.TryParse(modVersion, out var newVersion) && newVersion <= existingVersion)
                    {
                        MessageBox.Show($"Mod {modName} is already installed with an equal or newer version. Installation aborted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Directory.Delete(tempDir, true);
                        return;
                    }
                    Directory.Delete(modTargetPath, true);
                    LoadMods();
                }

                var targetManifestPath = Path.Combine(modTargetPath, "mod.manifest");
                Directory.CreateDirectory(modTargetPath);
                File.Move(manifestPath, targetManifestPath);
                CopyModFiles(Path.GetDirectoryName(manifestPath), modTargetPath);

                SaveModVersion(modId, modVersion, modNumber, Path.GetFileName(filePath));

                Mods.Add(new Mod { Id = modId, Name = modName, Version = modVersion, Path = modTargetPath, ModNumber = modNumber, IsEnabled = false });
                StatusLabel.Content = $"Added mod: {modName} (Version: {modVersion})";

                CheckForModUpdatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
        



        private void CopyModFiles(string sourceDir, string targetDir)
        {
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                Directory.CreateDirectory(targetSubDir);
                foreach (var file in Directory.GetFiles(dir))
                {
                    var targetFile = Path.Combine(targetSubDir, Path.GetFileName(file));
                    File.Copy(file, targetFile, true);
                }
            }
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }
        }

        // Utility method to copy all files and subdirectories from one directory to another
        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                Directory.CreateDirectory(destSubDir);
                CopyDirectory(subDir, destSubDir);
            }
        }


        private void SaveModOrder()
        {
            var modOrderPath = Path.Combine(ModFolder, "mod_order.txt");
            var modOrder = Mods.Select(mod => mod.IsEnabled ? mod.Id : $"# {mod.Id}").ToList();
            File.WriteAllLines(modOrderPath, modOrder);
            StatusLabel.Content = "Mod order saved.";
        }



        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Game Executable (*.exe)|*.exe",
                Title = "Select Kingdom Come Deliverance 2 Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GamePath = openFileDialog.FileName;
                SaveGamePath();
                //GamePathTextBox.Text = GamePath;
                StatusLabel.Content = "Game path updated.";
            }
        }



        private void ExtractArchive(string archivePath, string destination)
        {
            using (var archive = ArchiveFactory.Open(archivePath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(destination, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            // Check if the button is properly associated with a mod
            if (sender is Button button && button.DataContext is Mod mod)
            {
                // Verify the mod's path
                if (!string.IsNullOrEmpty(mod.Path) && Directory.Exists(mod.Path))
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", mod.Path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The mod folder does not exist or is not specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("No valid mod selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        private string CorrectXmlVersionInFile(string manifestPath)
        {
            string xmlContent = File.ReadAllText(manifestPath);

            // Überprüfe und ersetze die XML-Version, falls sie 2.0 ist
            if (xmlContent.Contains("<?xml version=\"2.0\""))
            {
                xmlContent = xmlContent.Replace("<?xml version=\"2.0\"", "<?xml version=\"1.0\"");
                // Überschreibe die Datei mit dem korrigierten Inhalt
                File.WriteAllText(manifestPath, xmlContent);
            }
            return xmlContent;
        }

        private Tuple<string, string, string> ParseManifest(string manifestPath)
        {
            try
            {
                string xmlContent = CorrectXmlVersionInFile(manifestPath);
                var doc = XDocument.Parse(xmlContent);

                var infoElement = doc.Descendants("info").FirstOrDefault();
                if (infoElement == null)
                    return null;

                var name = infoElement.Element("name")?.Value?.Trim();
                var version = infoElement.Element("version")?.Value?.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
                    return null;

                // Prüfen, ob bereits ein modid vorhanden ist
                var modidElement = infoElement.Element("modid");
                string id;
                if (modidElement != null && !string.IsNullOrWhiteSpace(modidElement.Value))
                {
                    // Vorhandener Wert wird übernommen
                    id = modidElement.Value.Trim();
                }
                else
                {
                    // Erzeuge modid basierend auf dem Namen
                    id = GenerateModId(name);
                    if (modidElement == null)
                    {
                        infoElement.Add(new XElement("modid", id));
                    }
                    else
                    {
                        modidElement.Value = id;
                    }
                }

                // Speichere das Manifest, falls Änderungen vorgenommen wurden
                File.WriteAllText(manifestPath, doc.ToString());

                return Tuple.Create(id, name, version);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GenerateModId(string name)
        {
            // Generiere modid aus dem Namen: nur englische Kleinbuchstaben (a-z)
            string id = Regex.Replace(name.ToLowerInvariant(), @"[^a-z]", "");
            // Fallback: Falls das Ergebnis leer ist, z. B. weil der Name nur Sonderzeichen enthält
            if (string.IsNullOrEmpty(id))
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(name);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    string fallback = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    fallback = Regex.Replace(fallback, @"[^a-z]", "");
                    if (fallback.Length > 8)
                        id = fallback.Substring(0, 8);
                    else if (fallback.Length > 0)
                        id = fallback;
                    else
                        id = "moddefault"; // Fester Fallback, falls gar nichts übrig bleibt
                }
            }
            return id;
        }






        private string GenerateManifest(string folderPath)
        {
            // Verwende den Ordnernamen als Basis (z.B. "Radovan Master Teacher-160-1-0-1738905930 (2)")
            var folderInfo = new DirectoryInfo(folderPath);
            string originalName = folderInfo.Name;

            // Versuche, aus dem Namen den Mod-Namen und die Version zu extrahieren.
            // Regex-Erklärung:
            // ^(.*?)-\d+-(\d+)-(\d+)-.*$
            //   Gruppe 1: Mod-Name (non-greedy bis zum ersten Bindestrich, der auf Zahlen folgt)
            //   Gruppe 2: Versionsmajor
            //   Gruppe 3: Versionsminor
            string modNameSuggestion = originalName;
            string versionSuggestion = "N/A";
            Regex regex = new Regex(@"^(.*?)-\d+-(\d+)-(\d+)-.*$");
            Match match = regex.Match(originalName);
            if (match.Success)
            {
                modNameSuggestion = match.Groups[1].Value.Trim();
                versionSuggestion = $"{match.Groups[2].Value}.{match.Groups[3].Value}";
            }
            else
            {
                // Falls kein passendes Muster gefunden wurde, entferne am Ende alle Zahlen, Bindestriche und Klammern.
                modNameSuggestion = Regex.Replace(originalName, @"[-\s\d\(\)]+$", "").Trim();
            }

            // Verwende den NameInputDialog, um dem Benutzer den vorgeschlagenen Mod-Namen anzuzeigen und ggf. anzupassen.
            //NameInputDialog dialog = new NameInputDialog(modNameSuggestion);
            //if (dialog.ShowDialog() == true)
            //{
            //    modNameSuggestion = dialog.EnteredName;
            //}
            
            
            
            // Generiere die modid: Nur englische Kleinbuchstaben (alle anderen Zeichen werden entfernt)
            string modId = Regex.Replace(modNameSuggestion.ToLowerInvariant(), @"[^a-z]", "");

            // Erstelle den XML-Inhalt für das Manifest
            string manifestContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<kcd_mod>
  <info>
    <name>{modNameSuggestion}</name>
    <description>auto generated manifest from kcd2 modmanager</description>
    <author>N/A</author>
    <version>{versionSuggestion}</version>
    <modid>{modId}</modid>
  </info>
</kcd_mod>";

            // Speichere das Manifest im Root des Mod-Ordners
            string manifestPath = Path.Combine(folderPath, "mod.manifest");
            File.WriteAllText(manifestPath, manifestContent);

            return manifestPath;
        }


        private void ForceUIRefresh()
        {
            // Erzwinge ein vollständiges Redraw des Fensters
            var current = this.Content;
            this.Content = null;
            this.Content = current;
        }

        private void UpdateTheme()
        {
            if (isDarkMode)
            {
                // Dark Mode
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                this.Resources["ModListItemEvenBrush"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                this.Resources["ModListItemOddBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                this.Resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["SelectedItemBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 120));

                // Extra brushes for search bar and clear button
                this.Resources["SearchBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                this.Resources["SearchForegroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["SearchBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                this.Resources["ClearButtonBrush"] = new SolidColorBrush(Colors.White);

                this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                StatusLabel.Foreground = Brushes.White;
                ModsEnabledCount.Foreground = Brushes.White;

                ForceUIRefresh();
            }
            else
            {
                // Light Mode
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ListBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ModListItemEvenBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ModListItemOddBrush"] = new SolidColorBrush(Colors.LightGray);
                this.Resources["ListBoxForegroundBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["SelectedItemBrush"] = new SolidColorBrush(Colors.LightBlue);

                // Extra brushes for search bar and clear button
                this.Resources["SearchBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["SearchForegroundBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["SearchBorderBrush"] = new SolidColorBrush(Colors.Gray);
                this.Resources["ClearButtonBrush"] = new SolidColorBrush(Colors.Black);

                this.Background = (Brush)this.Resources["WindowBackgroundBrush"];
                StatusLabel.Foreground = Brushes.Black;
                ModsEnabledCount.Foreground = Brushes.Green;

                ForceUIRefresh();
            }
        }





        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            // Dark Mode-Zustand umschalten
            isDarkMode = !isDarkMode;
            // Speichere den aktuellen Zustand in den Settings
            Settings.Default.IsDarkMode = isDarkMode;
            Settings.Default.Save();

            // Aktualisiere das Theme
            UpdateTheme();
        }




        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                        ProcessModFolder(file);
                    else
                        ProcessModFile(file);
                }
                SaveModOrder();
            }
        }

        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            if (sender is FrameworkElement element && element.DataContext is Mod mod)
                ModList.SelectedItem = mod;
        }

        private void ModList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(null);
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is ListBox listBox && listBox.SelectedItem is Mod mod)
                    {
                        // Drag & Drop nur erlauben, wenn nach Load Order sortiert wird
                        if (!sortByLoadOrder) return;
                        DragDrop.DoDragDrop(listBox, mod, DragDropEffects.Move);
                    }
                }
            }
        }

        private void ModList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                DependencyObject clickedElement = e.OriginalSource as DependencyObject;
                while (clickedElement != null && clickedElement != listBox)
                {
                    if (clickedElement is Button || clickedElement is CheckBox)
                        return;
                    clickedElement = VisualTreeHelper.GetParent(clickedElement);
                }
                Point clickPosition = e.GetPosition(listBox);
                var clickedItem = listBox.InputHitTest(clickPosition) as FrameworkElement;
                if (clickedItem?.DataContext is Mod clickedMod)
                    listBox.SelectedItem = clickedMod;
            }
        }

        private void ModList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Mod)) is Mod draggedMod && sender is ListBox listBox)
            {
                // Nur Drag & Drop umsetzen, wenn sortByLoadOrder aktiv ist
                if (!sortByLoadOrder) return;

                Point dropPosition = e.GetPosition(listBox);
                var targetMod = listBox.InputHitTest(dropPosition) as FrameworkElement;
                if (targetMod?.DataContext is Mod targetModData)
                {
                    int targetIndex = Mods.IndexOf(targetModData);
                    int draggedIndex = Mods.IndexOf(draggedMod);
                    Mods.Move(draggedIndex, targetIndex);
                    SaveModOrder();
                    RefreshAlternationIndexes();
                }
                listBox.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void ModCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is Mod mod)
            {
                mod.IsEnabled = checkBox.IsChecked ?? false;
                SaveModOrder();
                UpdateModsEnabledCount();
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            // Prüfen, ob nach Load Order sortiert wird
            if (!sortByLoadOrder)
            {
                MessageBox.Show("You can only change the mod order while sorting by load order.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button button && button.DataContext is Mod mod)
            {
                int index = Mods.IndexOf(mod);
                if (index > 0)
                {
                    Debug.WriteLine(index);
                    Mods.Move(index, index - 1);
                    SaveModOrder();
                    RefreshAlternationIndexes();
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            // Prüfen, ob nach Load Order sortiert wird
            if (!sortByLoadOrder)
            {
                MessageBox.Show("You can only change the mod order while sorting by load order.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button button && button.DataContext is Mod mod)
            {
                int index = Mods.IndexOf(mod);
                if (index < Mods.Count - 1)
                {
                    Mods.Move(index, index + 1);
                    SaveModOrder();
                    RefreshAlternationIndexes();
                }
            }
        }

        private void RefreshAlternationIndexes()
        {
            ModList.Items.Refresh();
        }



        // MOD Note: Kontextmenü-Handler zum Hinzufügen eines Kommentars für einen Mod
        private void ModContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Mod mod)
            {
                // Retrieve current note from JSON (if available)
                string currentNote = modNotes.ContainsKey(mod.Id) ? modNotes[mod.Id] : "";

                // Use the NameInputDialog for mod note editing.
                NameInputDialog dialog = new NameInputDialog(currentNote);
                dialog.Prompt = "Mod note:"; // Set the prompt text
                dialog.Owner = this;
                dialog.Title = "Mod note";
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    string newNote = dialog.EnteredText;
                    mod.Note = newNote;
                    modNotes[mod.Id] = newNote;
                    SaveModNotes();
                }
            }
        }

        private void ModOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                // Ensure the DataContext is passed to the ContextMenu
                btn.ContextMenu.DataContext = btn.DataContext;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ChangeModName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is Mod mod)
            {
                // Open the input dialog with the current mod name as default
                NameInputDialog dialog = new NameInputDialog(mod.Name);
                dialog.Prompt = "Enter mod name:"; // Sets the prompt label inside the dialog
                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    string newName = dialog.EnteredText;
                    mod.Name = newName;

                    // Recalculate modid based on new name (only letters, lowercase)
                    string newModId = Regex.Replace(newName.ToLowerInvariant(), @"[^a-z]", "");

                    // Update the manifest file
                    string manifestPath = Path.Combine(mod.Path, "mod.manifest");
                    if (File.Exists(manifestPath))
                    {
                        XDocument doc = XDocument.Load(manifestPath);
                        var infoElement = doc.Descendants("info").FirstOrDefault();
                        if (infoElement != null)
                        {
                            // Update or add the <name> element
                            var nameElement = infoElement.Element("name");
                            if (nameElement != null)
                            {
                                nameElement.Value = newName;
                            }
                            else
                            {
                                infoElement.Add(new XElement("name", newName));
                            }

                            // Update or add the <modid> element
                            var modidElement = infoElement.Element("modid");
                            if (modidElement != null)
                            {
                                modidElement.Value = newModId;
                            }
                            else
                            {
                                infoElement.Add(new XElement("modid", newModId));
                            }

                            doc.Save(manifestPath);
                        }
                    }
                    // Optionally update the mod's Id if it should reflect the new modid

                    RefreshAlternationIndexes();
                    mod.Id = newModId;
                }
            }
        }


        private void ChangeModNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is Mod mod)
            {
                // Aktuelle Notiz aus JSON abrufen, falls vorhanden
                string currentNote = modNotes.ContainsKey(mod.Id) ? modNotes[mod.Id] : "";

                // Eingabe-Dialog für die Mod-Notiz öffnen
                NameInputDialog dialog = new NameInputDialog(currentNote);
                dialog.Prompt = "Enter note for mod:"; // Setzt den Prompt-Text
                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    string newNote = dialog.EnteredText;
                    mod.Note = newNote;
                    modNotes[mod.Id] = newNote;
                    SaveModNotes();
                }
            }
        }




        private void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Mod mod)
            {
                // Prüfe, ob die Sicherheitsabfrage übersprungen werden soll
                if (!Settings.Default.DontAskOnDelete)
                {
                    var confirmationWindow = new DeleteConfirmationWindow();
                    confirmationWindow.Owner = this; // Setze den Hauptfensterbesitzer
                    confirmationWindow.ShowDialog();

                    if (!confirmationWindow.UserConfirmed)
                        return;

                    // Speichere die Option "Don't ask again"
                    if (confirmationWindow.DontAskAgain)
                    {
                        Settings.Default.DontAskOnDelete = true;
                        Settings.Default.Save();
                    }
                }

                try
                {
                    // Mod-Ordner löschen
                    if (Directory.Exists(mod.Path))
                    {
                        Directory.Delete(mod.Path, true);
                    }

                    // Mod aus der Liste entfernen
                    Mods.Remove(mod);

                    // Mod-Reihenfolge speichern
                    SaveModOrder();

                    StatusLabel.Content = $"Deleted mod: {mod.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete mod: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Failed to identify the mod to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableDeleteConfirmation_Click(object sender, RoutedEventArgs e)
        {
            // Lösche die Einstellung für "Don't ask again"
            Settings.Default.DontAskOnDelete = false;
            Settings.Default.Save();
            MessageBox.Show("Delete confirmation has been re-enabled.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void ToggleDevMode_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.IsDevMode = !Settings.Default.IsDevMode;
            Settings.Default.Save();
            MessageBox.Show($"Dev Mode is now {(Settings.Default.IsDevMode ? "enabled" : "disabled")}.", "Dev Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }




    }

    public class Mod
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }
        public bool IsEnabled { get; set; }
        public string Note { get; set; }
        public int Number { get; set; }
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; }
        public int ModNumber { get; set; }


        public Visibility UpdateVisibility => HasUpdate ? Visibility.Visible : Visibility.Collapsed;
    }

    public class NexusModFile
    {
        public int file_id { get; set; }
        public string version { get; set; }
        public long uploaded_timestamp { get; set; }
        public string name { get; set; }          // Must be present
        public string mod_version { get; set; }     // Must be present
    }


    public class NexusModFileUpdate
    {
        public int old_file_id { get; set; }
        public int new_file_id { get; set; }
        public string old_file_name { get; set; }
        public string new_file_name { get; set; }
        public long uploaded_timestamp { get; set; }
    }

    public class NexusModFilesResponse
    {
        public List<NexusModFile> files { get; set; }
        // Hier als Liste von NexusModFileUpdate anpassen:
        public List<NexusModFileUpdate> file_updates { get; set; }
    }
}
