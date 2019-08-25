﻿using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// The <c>arcpds</c> class deals with all operations involving the ArcDPS add-on and its plugins.
    /// </summary>
    public class arcdps
    {
        string game_path;

        string arc_templates_name;
        string arc_name;

        /* arc URLs */
        string arc_url = "https://www.deltaconnected.com/arcdps/x64/d3d9.dll";
        string buildtemplates_url = "https://www.deltaconnected.com/arcdps/x64/buildtemplates/d3d9_arcdps_buildtemplates.dll";
        string md5_hash_url = "https://www.deltaconnected.com/arcdps/x64/d3d9.dll.md5sum";

        UpdatingViewModel theView;

        /// <summary>
        /// The constructor sets several values to be used and also updates the UI to indicate that ArcDPS is the current task.
        /// </summary>
        /// <param name="arc_name">The name of the arcdps plugin file.</param>
        /// <param name="arc_templates_name">The name of the arcdps build templates plugin file.</param>
        /// <param name="viewModel">An instance of the <typeparamref>UpdatingViewModel</typeparamref> class serving as the DataContext for the application UI.</param>
        public arcdps(string arc_name, string arc_templates_name, UpdatingViewModel viewModel)
        {
            theView = viewModel;
            theView.showProgress = 0;
            /* display message over progress bar */
            theView.label = "Checking for ArcDPS updates";
            game_path = (string)Application.Current.Properties["game_path"];
            this.arc_name = arc_name;
            this.arc_templates_name = arc_templates_name;
        }

        /// <summary>
        /// Disables ArcDPS by moving its plugin into the 'Disabled Plugins' application subfolder.
        /// </summary>
        /// <param name="game_path">The Guild Wars 2 game path.</param>
        public static void disable(string game_path)
        {
            dynamic config_obj = configuration.getConfig();
            if (config_obj.installed.arcdps != null)
                File.Move(game_path + "\\bin64\\" + config_obj.installed.arcdps, "Disabled Plugins\\arcdps.dll");

            config_obj.disabled.arcdps = true;
            configuration.setConfig(config_obj);
        }

        /// <summary>
        /// Enables ArcDPS by moving its plugin back into the game's /bin64/ folder.
        /// </summary>
        /// <param name="game_path">The Guild Wars 2 game path.</param>
        public static void enable(string game_path)
        {
            dynamic config_obj = configuration.getConfig();
            if ((bool)config_obj.disabled.arcdps && config_obj.installed.arcdps != null)
                File.Move("Disabled Plugins\\arcdps.dll", game_path + "\\bin64\\" + config_obj.installed.arcdps);

            config_obj.disabled.arcdps = false;
            configuration.setConfig(config_obj);
        }


        /***************************** DELETING *****************************/
        /// <summary>
        /// Deletes the ArcDPS and ArcDPS build templates plugins as well as the /addons/arcdps game subfolder.
        /// </summary>
        /// <param name="game_path">The Guild Wars 2 game path.</param>
        public static void delete(string game_path)
        {
            /* if the /addons/ subdirectory exists for this add-on, delete it */
            if(Directory.Exists(game_path + "\\addons\\arcdps"))
                Directory.Delete(game_path + "\\addons\\arcdps", true);

            dynamic config_obj = configuration.getConfig();

            /* if a .dll is associated with the add-on, delete it */
            if (config_obj.installed.arcdps != null)
            {
                File.Delete(game_path + "\\bin64\\" + config_obj.installed.arcdps);
                File.Delete(game_path + "\\bin64\\" + config_obj.arcdps_buildTemplates);
            }
                

            /* if the .dll is in the 'Disabled Plugins' folder, delete it */
            if (File.Exists("Disabled Plugins\\arcdps.dll"))
                File.Delete("Disabled Plugins\\arcdps.dll");

            config_obj.version.arcdps = null;       //no installed version
            config_obj.installed.arcdps = null;     //no installed .dll name
            configuration.setConfig(config_obj);    //writing to config.ini
        }


        /***************************** UPDATING *****************************/
        /// <summary>
        /// Asynchronously checks for and installs updates for ArcDPS and checks to ensure that ArcDPS Build Templates are installed.
        /// </summary>
        /// <returns>A <c>Task</c> that can be awaited.</returns>
        public async Task update()
        {
            /* download md5 file from arc website */
            var client = new WebClient();
            string md5 = client.DownloadString(md5_hash_url);
            /* if arc is not installed or recorded timestamp is different from latest release, download it */
            dynamic config_obj = configuration.getConfig();
            if (config_obj.version.arcdps == null || config_obj.version.arcdps != md5)
            {
                theView.label = "Downloading ArcDPS";
                client.DownloadProgressChanged += arc_DownloadProgressChanged;
                client.DownloadFileCompleted += arc_DownloadCompleted;
                await client.DownloadFileTaskAsync(new System.Uri(arc_url), game_path + "\\bin64\\" + arc_name);

                config_obj.installed.arcdps = arc_name;
                config_obj.version.arcdps = md5;
                configuration.setConfig(config_obj);
            }
            else
            {
                Application.Current.Properties["ArcDPS"] = false;
                theView.showProgress = 100;
            }

            /* build templates - temporary, just checks for file existence */
            if (!File.Exists(game_path + "\\bin64\\" + arc_templates_name))
            {
                await update_templates();
            }
                
        }

        void arc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            theView.showProgress = e.ProgressPercentage;
        }

        void arc_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Application.Current.Properties["ArcDPS"] = false;
        }

        /***************************** ArcDPS Build Templates *****************************/
        /// <summary>
        /// Downloads the ArcDPS Build Templates plugin to the game's /bin64/ folder.
        /// </summary>
        /// <returns>A <c>Task</c> that can be awaited.</returns>
        //currently doesn't version check, just downloads
        public async Task update_templates()
        {
            theView.label = "Updating ArcDPS Build Templates";
            var client = new WebClient();

            client.DownloadProgressChanged += arc_buildTemplates_DownloadProgressChanged;
            client.DownloadFileCompleted += arc_buildTemplates_DownloadCompleted;
            await client.DownloadFileTaskAsync(new System.Uri(buildtemplates_url), game_path + "\\bin64\\" + arc_templates_name);
            Application.Current.Properties["ArcDPS"] = false;
        }

        void arc_buildTemplates_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            theView.showProgress = e.ProgressPercentage;
        }

        void arc_buildTemplates_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Application.Current.Properties["ArcDPS"] = false;
            theView.showProgress = 100;
        }

    }
}
