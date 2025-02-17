﻿using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace GW2_Addon_Manager
{
    class LoaderSetup
    {
        static string loader_git_url = "https://api.github.com/repos/gw2-addon-loader/loader-core/releases/latest";
        string loader_game_path;

        UpdatingViewModel viewModel;
        string fileName;
        UserConfig userConfig;
        string latestLoaderVersion;

        
        /// <summary>
        /// Sets some UI text to indicate that the addon loader is having an update check
        /// </summary>
        /// <param name="aViewModel"></param>
        public LoaderSetup(UpdatingViewModel aViewModel)
        {
            viewModel = aViewModel;
            viewModel.label = "Checking for updates to Addon Loader";
            userConfig = Configuration.getConfigAsYAML();
            loader_game_path = Path.Combine(userConfig.game_path, userConfig.bin_folder);
        }

        /// <summary>
        /// Checks for update to addon loader and downloads if a new release is available
        /// </summary>
        /// <returns></returns>
        public async Task handleLoaderInstall()
        {
            dynamic releaseInfo = UpdateHelpers.GitReleaseInfo(loader_git_url);

            latestLoaderVersion = releaseInfo.tag_name;

            if (userConfig.loader_version == latestLoaderVersion)
                return;

            string downloadLink = releaseInfo.assets[0].browser_download_url;
            await Download(downloadLink);
        }

        // Downloads file from the url parameter to the user's temporary files folder and prompts install.
        private async Task Download(string url)
        {
            viewModel.label = "Downloading Addon Loader";
            var client = new WebClient();
            client.Headers.Add("User-Agent", "request");

            fileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));

            if (File.Exists(fileName))
                File.Delete(fileName);

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(loader_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(loader_DownloadCompleted);

            await client.DownloadFileTaskAsync(new System.Uri(url), fileName);
            Install();
        }


        // Installs the addon loader from the temporary files folder.
        private void Install()
        {
            viewModel.label = "Installing Addon Loader";
            string loader_destination = Path.Combine(loader_game_path, "d3d9.dll");

            if (File.Exists(loader_destination))
                File.Delete(loader_destination);
            
            ZipFile.ExtractToDirectory(fileName, loader_game_path);

            userConfig.loader_version = latestLoaderVersion;

            Configuration.setConfigAsYAML(userConfig);
        }


        /***** DOWNLOAD EVENTS *****/
        void loader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            viewModel.showProgress = e.ProgressPercentage;
        }

        void loader_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }

    }
}
