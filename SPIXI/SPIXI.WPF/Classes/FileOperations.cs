using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        saveFileDialog.Filter = "Ixian Wallet (*.wal)|*.wal";
        saveFileDialog.FileName = "wallet";
        saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (saveFileDialog.ShowDialog() == true)
        {
            System.IO.File.Copy(filepath, saveFileDialog.FileName, true);
        }

        return Task.FromResult(true);
    }
}