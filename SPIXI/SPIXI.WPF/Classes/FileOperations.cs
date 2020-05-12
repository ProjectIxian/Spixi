using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(FileOperations_WPF))]


public class FileOperations_WPF : IFileOperations
{
    public Task share(string filepath, string title)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        //saveFileDialog.Filter = "Ixian Wallet (*.wal)|*.wal";
        saveFileDialog.FileName = Path.GetFileName(filepath);
        saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (saveFileDialog.ShowDialog() == true)
        {
            System.IO.File.Copy(filepath, saveFileDialog.FileName, true);
        }

        return Task.FromResult(true);
    }

    public void open(string filepath)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        Device.OpenUri(new Uri(filepath));
#pragma warning restore CS0618 // Type or member is obsolete
    }
}