using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Network;
using SPIXI.Storage;
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
        string selectedLanguage = null;

        public SettingsPage()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "settings.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setNickname", Node.localStorage.nickname);
            int activeAppearance = (int)ThemeManager.getActiveAppearance();
            Utils.sendUiCommand(webView, "setAppearance", activeAppearance.ToString()); 

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

            if (onNavigatingGlobal(current_url))
            {
                e.Cancel = true;
                return;
            }

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                OnBackButtonPressed();
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert(SpixiLocalization._SL("settings-emptynick-title"), SpixiLocalization._SL("settings-emptynick-text"), SpixiLocalization._SL("global-dialog-ok"));
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
            else if (current_url.Equals("ixian:deleted", StringComparison.Ordinal))
            {
                onDeleteDownloads();
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
            else if (current_url.StartsWith("ixian:language:", StringComparison.Ordinal))
            {
                string lang = current_url.Substring("ixian:language:".Length);
                if(SpixiLocalization.loadLanguage(lang))
                {
                    selectedLanguage = lang;
                    loadPage(webView, "settings.html");
                }else
                {
                    selectedLanguage = null;
                }
            }
            else if (current_url.StartsWith("ixian:appearance:", StringComparison.Ordinal))
            {
                string appearanceString = current_url.Substring("ixian:appearance:".Length);
                ThemeAppearance appearance = (ThemeAppearance)Convert.ToInt32(appearanceString);

                if (ThemeManager.changeAppearance(appearance))
                {
                    loadPage(webView, "settings.html");
                }
                
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
            if (selectedLanguage != null)
            {
                Application.Current.Properties["language"] = selectedLanguage;
                Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF
            }
            else
            {
                resetLanguage();
            }

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

        private void resetLanguage()
        {
            string lang = "en-us";
            if (Application.Current.Properties.ContainsKey("language"))
            {
                lang = Application.Current.Properties["language"] as string;
            }
            SpixiLocalization.loadLanguage(lang);
        }

        public void onDeleteWallet(object sender, EventArgs e)
        {
            if (IxianHandler.getWalletStorage().deleteWallet())
            {
                // Also delete the account
                onDeleteAccount();

                // Stop network activity
                Node.stop();

                Application.Current.Properties.Remove("onboardingComplete");
                Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

                SpixiLocalization.addCustomString("OnboardingComplete", "false");

                Node.localStorage.deleteTransactionCacheFile();
                TransactionCache.clearAllTransactions();
                Node.tiv.clearCache();

                // Show the launch page
                Navigation.PushAsync(new LaunchPage(), Config.defaultXamarinAnimations);

                // Remove the settings page
                Navigation.RemovePage(this);

                // Todo: also remove the parent page without causing memory leaks
            }
            else
            {
                displaySpixiAlert(SpixiLocalization._SL("settings-deletew-error-title"), SpixiLocalization._SL("settings-deletew-error-text"), SpixiLocalization._SL("global-dialog-ok"));
            }

        }

        public void onDeleteAccount()
        {
            Node.localStorage.deleteAllAvatars();
            Node.localStorage.deleteAccountFile();
            Node.localStorage.deleteAllDownloads();
            StreamProcessor.deletePendingMessages();
            FriendList.deleteEntireHistory();
            FriendList.deleteAccounts();
            FriendList.clear();

            displaySpixiAlert(SpixiLocalization._SL("settings-deleteda-title"), SpixiLocalization._SL("settings-deleteda-text"), SpixiLocalization._SL("global-dialog-ok"));
        }

        public void onDeleteHistory()
        {
            FriendList.deleteEntireHistory();
            displaySpixiAlert(SpixiLocalization._SL("settings-deletedh-title"), SpixiLocalization._SL("settings-deletedh-text"), SpixiLocalization._SL("global-dialog-ok"));
        }

        public void onDeleteDownloads()
        {
            try
            {
                TransferManager.resetIncomingTransfers();
                int file_count = 0;
                foreach (var file in Directory.EnumerateFiles(Path.Combine(Config.spixiUserFolder, "Downloads")))
                {
                    File.Delete(file);
                    file_count++;
                }
                displaySpixiAlert(SpixiLocalization._SL("settings-deletedd-title"), string.Format(SpixiLocalization._SL("settings-deletedd-text"), file_count), SpixiLocalization._SL("global-dialog-ok"));
            }
            catch (Exception e)
            {
                Logging.error("Exception while deleting downloads: " + e);
                displaySpixiAlert(SpixiLocalization._SL("settings-deleted-error-title"), SpixiLocalization._SL("settings-deleted-error-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
        }

        public async Task onChangeAvatarAsync(object sender, EventArgs e)
        {
            var picker_service = DependencyService.Get<IFilePicker>();

            SpixiImageData spixi_img_data = await picker_service.PickImageAsync();
            Stream stream = spixi_img_data.stream;

            if (stream == null)
            {
                return;
            }

            var file_path = Path.Combine(Node.localStorage.avatarsPath, "avatar-tmp.jpg");
            try
            {
                byte[] image_bytes = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    stream.Close();
                    image_bytes = picker_service.ResizeImage(ms.ToArray(), 960, 960, 80);
                    if (image_bytes == null)
                    {
                        return;
                    }
                }

                FileStream fs = new FileStream(file_path, FileMode.OpenOrCreate, FileAccess.Write);
                fs.Write(image_bytes, 0, image_bytes.Length);
                fs.Close();
            }
            catch (Exception ex)
            {
                await displaySpixiAlert(SpixiLocalization._SL("intro-new-avatarerror-title"), ex.ToString(), SpixiLocalization._SL("global-dialog-ok"));
                return;
            }

            Utils.sendUiCommand(webView, "loadAvatar", file_path);
            Node.changedSettings = true;
        }

        // Applies the avatar image once the user chooses to Save changes
        public void applyAvatar()
        {
            var file_path = Node.localStorage.getOwnAvatarPath(false);
            var source_file_path = Path.Combine(Node.localStorage.avatarsPath, "avatar-tmp.jpg");

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

            FriendList.broadcastAvatarChange();
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
            resetLanguage();

            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}