﻿using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// Handles downloading a new version of the application.
    /// </summary>
    class SelfUpdate
    {
        static string applicationRepoUrl = "https://api.github.com/repos/fmmmlee/GW2-Addon-Manager/releases/latest";
        static string update_folder = "latestRelease";
        static string update_name = "update.zip";

        OpeningViewModel viewModel;

        /// <summary>
        /// Sets the viewmodel and starts the download of the latest release.
        /// </summary>
        /// <param name="appViewModel"></param>
        public SelfUpdate(OpeningViewModel appViewModel)
        {
            viewModel = appViewModel;
            viewModel.UpdateProgressVisibility = Visibility.Visible;
            viewModel.UpdateLinkVisibility = Visibility.Hidden;
            Task.Run(() => downloadLatestRelease());
        }

        /// <summary>
        /// Downloads the latest application release.
        /// </summary>
        /// <returns></returns>
        public async Task downloadLatestRelease()
        {
            //perhaps change this to check if downloaded update is latest or not
            if (Directory.Exists(update_folder))
                Directory.Delete(update_folder, true);

            dynamic latestInfo = UpdateHelpers.GitReleaseInfo(applicationRepoUrl);
            string downloadUrl = latestInfo.assets[0].browser_download_url;

            viewModel.UpdateAvailable = "Downloading " + latestInfo.tag_name;

            Directory.CreateDirectory(update_folder);
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "request");
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(selfUpdate_DownloadProgress);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(selfUpdate_DownloadCompleted);
            await client.DownloadFileTaskAsync(new System.Uri(downloadUrl), Path.Combine(update_folder, update_name));
        }

        void selfUpdate_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            viewModel.UpdateAvailable = "Download complete!";
            Application.Current.Properties["update_self"] = true;
        }

        void selfUpdate_DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            viewModel.UpdateDownloadProgress = e.ProgressPercentage;
        }

        /// <summary>
        /// Starts the self-updater if an application update has been downloaded.
        /// </summary>
        public static void startUpdater()
        {
            if(Application.Current.Properties["update_self"] != null && (bool)Application.Current.Properties["update_self"])
                Process.Start("UOAOM Updater.exe");
        }
    }
}
