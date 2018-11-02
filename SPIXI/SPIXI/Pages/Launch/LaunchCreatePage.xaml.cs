using DLT.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LaunchCreatePage : SpixiContentPage
	{
		public LaunchCreatePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/intro_new.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync();
            }
            /*else if (current_url.Equals("ixian:create", StringComparison.Ordinal))
            {
                onCreateAccount();
            }*/
            else if (current_url.Contains("ixian:create:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:create:" }, StringSplitOptions.None);
                string nick = split[1];
                onCreateAccount(nick);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI Account", "Please type your nickname.", "OK");
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
                //Navigation.PushAsync(new LaunchRestorePage());
                onChangeAvatarAsync(sender, e);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

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

                var filePath = Node.localStorage.getOwnAvatarPath();

                //DisplayAlert("Alert", filePath, "OK");

                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                stream.CopyTo(fs);
                /*using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    file.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }*/
            webView.Eval(string.Format("loadAvatar(\"{0}\")", filePath));
                stream.Close();
                fs.Close();

                /*
                var filename = System.IO.Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).ToString(), "NewFolder");
                filename = System.IO.Path.Combine(filename, "filename.jpg");
                using (var fileOutputStream = new FileOutputStream(filename))
                {
                    await fileOutputStream.WriteAsync(reducedImage);
                }*/

            }

        }

        public void onCreateAccount(string nick)
        {
            if (Node.generateWallet())
            {
//                DisplayAlert("Account Created", "Don't forget to save your private key!", "Ok");

                Node.localStorage.nickname = nick;
                Node.localStorage.writeAccountFile();
            }
            else
            {
                DisplayAlert("Error", "Cannot generate new wallet. Please try again.", "Ok");
                return;
            }

            //Navigation.PushAsync(new LaunchRestorePage());
            Navigation.PushAsync(new HomePage());
            Navigation.RemovePage(this);
        }


        /*
        public void onCreateAccount(object sender, EventArgs e)
        {
            if(nameInput.Text.Length < 1)
            {
                DisplayAlert("Error", "Please type a nickname.", "Ok");
                return;
            }

            if (Node.generateWallet())
            {
                DisplayAlert("Account Created", "Don't forget to save your private key!", "Ok");
            }
            else
            {
                DisplayAlert("Error", "Cannot generate new wallet. Please try again.", "Ok");
                return;
            }

            //Navigation.PopAsync();
            //Navigation.PopAsync();

            Navigation.PushAsync(new HomePage());
            Navigation.RemovePage(this);
        }*/
    }
}