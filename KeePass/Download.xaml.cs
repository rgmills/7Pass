﻿using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeePass.Data;
using KeePass.Properties;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace KeePass
{
    public partial class Download
    {
        public Download()
        {
            InitializeComponent();
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[32768];

            while (true)
            {
                var read = input.Read(buffer, 0, buffer.Length);

                if (read <= 0)
                    return;

                output.Write(buffer, 0, read);
            }
        }

        private void DownloadDatabase()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.NoNetwork);
                return;
            }
            
            UpdateControls(true);

            var client = new WebClient();
            client.OpenReadCompleted += client_OpenReadCompleted;
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.OpenReadAsync(new Uri(txtUrl.Text, UriKind.Absolute));

            progress.Value = 0;
            progress.IsIndeterminate = true;
        }

        private static bool IncreaseStorage(long quotaSizeDemand)
        {
            using (var store = IsolatedStorageFile
                .GetUserStoreForApplication())
            {
                var maxAvailableSpace = store.AvailableFreeSpace;

                if (quotaSizeDemand <= maxAvailableSpace)
                    return true;

                return store.IncreaseQuotaTo(
                    store.Quota + quotaSizeDemand);
            }
        }

        private void UpdateControls(bool isDownloading)
        {
            var visible = isDownloading
                ? Visibility.Visible
                : Visibility.Collapsed;

            lblLoad.Visibility = visible;
            progress.Visibility = visible;
            progress.IsIndeterminate = false;

            txtUrl.IsReadOnly = isDownloading;
            cmdDownload.IsEnabled = !isDownloading;
        }

        private void client_DownloadProgressChanged(
            object sender, DownloadProgressChangedEventArgs e)
        {
            progress.IsIndeterminate = false;
            progress.Value = e.ProgressPercentage;

            lblLoad.Text = string.Format(
                AppResources.Downloading,
                e.ProgressPercentage);
        }

        private void client_OpenReadCompleted(
            object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateControls(false);
                MessageBox.Show(
                    AppResources.DownloadError +
                    e.Error.Message);
                
                return;
            }

            var result = e.Result;
            progress.IsIndeterminate = true;
            lblLoad.Text = AppResources.Saving;

            if (result == null || result.Length == 0)
            {
                UpdateControls(false);
                MessageBox.Show(AppResources.NullDownload);

                return;
            }

            if (!IncreaseStorage(result.Length))
            {
                MessageBox.Show(AppResources.FileTooLarge);
                UpdateControls(false);

                return;
            }

            using (var store = IsolatedStorageFile
                .GetUserStoreForApplication())
            using (var fs = store.CreateFile(Consts.DATABASE))
            {
                CopyStream(result, fs);
            }

            AppSettingsService.Clear();
            NavigationService.GoBack();
        }

        private void cmdDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadDatabase();
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && cmdDownload.IsEnabled)
                DownloadDatabase();
        }

        private void txtUrl_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            cmdDownload.IsEnabled = Uri.IsWellFormedUriString(
                txtUrl.Text, UriKind.Absolute);
        }
    }
}