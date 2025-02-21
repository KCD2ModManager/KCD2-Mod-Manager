using System;
using System.Windows;
using System.Windows.Navigation;
using System.Web; // For HttpUtility

namespace KCD2_mod_manager
{
    public partial class NexusSSOLoginWindow : Window
    {
        // Public properties to expose the returned API key and username
        public string Token { get; private set; }
        public string Username { get; private set; }

        public NexusSSOLoginWindow()
        {
            InitializeComponent();
            // Replace these with your actual credentials and desired parameters.
            string clientId = "YOUR_CLIENT_ID";
            string redirectUri = "YOUR_REDIRECT_URI";
            string applicationSlug = "your_app_slug";

            // Construct the URL for Nexus SSO login.
            // Depending on the Nexus SSO implementation, this URL may vary.
            string url = $"https://www.nexusmods.com/sso?client_id={clientId}&redirect_uri={redirectUri}&application={applicationSlug}";
            webBrowser.Navigate(url);
        }

        private void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            // When the user successfully logs in, the SSO should redirect to your redirect_uri with query parameters.
            if (e.Uri != null && e.Uri.Query.Contains("api_key="))
            {
                // Parse the query string
                var query = HttpUtility.ParseQueryString(e.Uri.Query);
                Token = query["api_key"];
                Username = query["username"]; // if Nexus returns a username

                // Close the window with DialogResult = true to indicate a successful login
                DialogResult = true;
                Close();
            }
        }
    }
}
