using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : SpixiContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/settings.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setNickname", Node.localStorage.nickname);

            var filePath = Node.localStorage.getOwnAvatarPath();
            if (filePath.Equals("img/spixiavatar.png", StringComparison.Ordinal))
            {
                // No custom avatar has been chosen
            }
            else
            {
                // A custom avatar has been chosen previously
                Utils.sendUiCommand(webView, "showRemoveAvatar", "1");
            }

            Utils.sendUiCommand(webView, "loadAvatar", filePath);
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert("SPIXI Account", "Please type your nickname.", "OK");
            }
            else if (current_url.Equals("ixian:delete", StringComparison.Ordinal))
            {
                onDeleteWallet(sender, e);
            }
            else if (current_url.Equals("ixian:deletea", StringComparison.Ordinal))
            {
                onDeleteAccount();
            }
            else if (current_url.Equals("ixian:deleteh", StringComparison.Ordinal))
            {
                onDeleteHistory();
            }
            else if (current_url.Equals("ixian:backup", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new BackupPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Contains("ixian:save:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:save:" }, StringSplitOptions.None);
                string nick = split[1];
                onSaveSettings(nick);
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                onChangeAvatarAsync(sender, e);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else if (current_url.Equals("ixian:remove", StringComparison.Ordinal))
            {
                onRemoveAvatar();
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        public void onSaveSettings(string nick)
        {
            if (Node.localStorage.nickname != nick)
            {
                Node.localStorage.nickname = nick;
                FriendList.broadcastNicknameChange();
            }
            Node.localStorage.writeAccountFile();
            Node.changedSettings = true;
            applyAvatar();

            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        public void onDeleteWallet(object sender, EventArgs e)
        {
            if (Node.walletStorage.deleteWallet())
            {
                // Also delete the account
                onDeleteAccount();

                // Stop network activity
                Node.stop();

                // Start network activity
                Node.start();

                // Show the launch page
                Navigation.PushAsync(new LaunchPage(), Config.defaultXamarinAnimations);

                // Remove the settings page
                Navigation.RemovePage(this);

                // Todo: also remove the parent page without causing memory leaks
            }
            else
            {
                displaySpixiAlert("Error", "Cannot delete wallet file.", "OK");
            }

        }

        public void onDeleteAccount()
        {
            Node.localStorage.deleteAccountFile();
            FriendList.deleteEntireHistory();
            FriendList.clear();

            displaySpixiAlert("Done", "This account is now empty.", "OK");
        }

        public void onDeleteHistory()
        {
            FriendList.deleteEntireHistory();
            displaySpixiAlert("Done", "Entire messages history deleted.", "OK");
        }

        public async Task onChangeAvatarAsync(object sender, EventArgs e)
        {
            Stream stream = await DependencyService.Get<IPicturePicker>().GetImageStreamAsync();
            if (stream != null)
            {
                Image image = new Image
                {
                    Source = ImageSource.FromStream(() => stream),
                    BackgroundColor = Color.Gray
                };

                var filePath = Path.Combine(Node.localStorage.getTmpPath(), "avatar-tmp.jpg");

                try
                {
                    FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                    stream.CopyTo(fs);

                    stream.Close();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    await displaySpixiAlert("Error", ex.ToString(), "ok");
                    return;
                }

                Utils.sendUiCommand(webView, "loadAvatar", filePath);
                Utils.sendUiCommand(webView, "showRemoveAvatar", "1");
                Node.changedSettings = true;
            }

        }

        // Applies the avatar image once the user chooses to Save changes
        public void applyAvatar()
        {
            var file_path = Node.localStorage.getOwnAvatarPath(false);
            var source_file_path = Path.Combine(Node.localStorage.getTmpPath(), "avatar-tmp.jpg");

            // Check if the source file exists before proceeding
            if (!File.Exists(source_file_path))
            {
                return;
            }

            // Remove the avatar image first
            if (File.Exists(file_path))
            {
                File.Delete(file_path);
            }

            File.Copy(source_file_path, file_path);

            // Delete the temporary avatar image if the copy was successfull
            if (File.Exists(file_path))
            {
                File.Delete(source_file_path);
            }
        }

        public void onRemoveAvatar()
        {
            if (Node.localStorage.deleteOwnAvatar())
            {
                Utils.sendUiCommand(webView, "showRemoveAvatar", "0");
                Utils.sendUiCommand(webView, "loadAvatar", Node.localStorage.getOwnAvatarPath());
                Node.changedSettings = true;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}