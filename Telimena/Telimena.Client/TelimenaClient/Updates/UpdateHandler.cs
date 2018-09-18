﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Telimena.Client
{
    internal partial class UpdateHandler
    {
        public UpdateHandler(IMessenger messenger, ProgramInfo programInfo, bool suppressAllErrors, IReceiveUserInput inputReceiver
            , IInstallUpdates updateInstaller)
        {
            this.Messenger = messenger;
            this.ProgramInfo = programInfo;
            this.SuppressAllErrors = suppressAllErrors;
            this.InputReceiver = inputReceiver;
            this.UpdateInstaller = updateInstaller;
        }

        public const string UpdaterFileName = "Updater.exe";

        protected string UpdatesFolderName => this.ProgramInfo?.Name + " Updates";

        private IMessenger Messenger { get; }
        private ProgramInfo ProgramInfo { get; }
        private bool SuppressAllErrors { get; }
        private IReceiveUserInput InputReceiver { get; }
        private IInstallUpdates UpdateInstaller { get; }

        private string BasePath => AppDomain.CurrentDomain.BaseDirectory;

        public async Task DownloadUpdatePackages(IReadOnlyList<UpdatePackageData> packagesToDownload)
        {
            try
            {
                List<Task> downloadTasks = new List<Task>();
                DirectoryInfo updatesFolder = this.GetUpdatesSubfolder(packagesToDownload, this.BasePath);
                Directory.CreateDirectory(updatesFolder.FullName);

                foreach (UpdatePackageData updatePackageData in packagesToDownload)
                {
                    downloadTasks.Add(this.StoreUpdatePackage(updatePackageData, updatesFolder));
                }

                await Task.WhenAll(downloadTasks);
            }
            catch (Exception)
            {
                if (!this.SuppressAllErrors)
                {
                    throw;
                }
            }
        }

        public async Task HandleUpdates(UpdateResponse response, BetaVersionSettings betaVersionSettings)
        {
            try
            {
                IReadOnlyList<UpdatePackageData> packagesToInstall = null;
                if (response.UpdatePackages == null || !response.UpdatePackages.Any())
                {
                    return;
                }

                packagesToInstall = response.UpdatePackages;
                //this.DeterminePackagesToInstall(response, betaVersionSettings);

                if (packagesToInstall != null && packagesToInstall.Any())
                {
                    await this.DownloadUpdatePackages(packagesToInstall);

                    FileInfo instructionsFile = UpdateInstructionCreator.CreateInstructionsFile(packagesToInstall, this.ProgramInfo);

                    bool installUpdatesNow = this.InputReceiver.ShowInstallUpdatesNowQuestion(packagesToInstall);
                    FileInfo updaterFile = PathFinder.GetUpdaterExecutable(this.BasePath, this.UpdatesFolderName);
                    if (installUpdatesNow)
                    {
                        this.UpdateInstaller.InstallUpdates(instructionsFile, updaterFile);
                    }
                }
            }
            catch (Exception)
            {
                if (!this.SuppressAllErrors)
                {
                    throw;
                }
            }
        }

        protected async Task StoreUpdatePackage(UpdatePackageData pkgData, DirectoryInfo updatesFolder)
        {
            Stream stream = await this.Messenger.DownloadFile(ApiRoutes.DownloadUpdatePackage + "?id=" + pkgData.Id);
            string pkgFilePath = Path.Combine(updatesFolder.FullName, pkgData.FileName);
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pkgFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream).ContinueWith(copyTask => { fileStream.Close(); });
                pkgData.StoredFilePath = pkgFilePath;
            }
            catch
            {
                fileStream?.Close();

                throw;
            }
        }

        //private IReadOnlyList<UpdatePackageData> DeterminePackagesToInstall(UpdateResponse response, BetaVersionSettings betaVersionSettings)
        //{
        //    IReadOnlyList<UpdatePackageData> packagesToInstall = null;
        //    if (betaVersionSettings == BetaVersionSettings.UseBeta)
        //    {
        //        packagesToInstall = response.UpdatePackagesIncludingBeta;
        //    }
        //    else if (betaVersionSettings == BetaVersionSettings.IgnoreBeta)
        //    {
        //        packagesToInstall = response.UpdatePackages;
        //    }
        //    else if (betaVersionSettings == BetaVersionSettings.AskUserEachTime && response.UpdatePackagesIncludingBeta.Any(x => x.IsBeta))
        //    {
        //        bool includeBetaPackages = this.InputReceiver.ShowIncludeBetaPackagesQuestion(response);
        //        if (includeBetaPackages)
        //        {
        //            packagesToInstall = response.UpdatePackagesIncludingBeta;
        //        }
        //        else
        //        {
        //            packagesToInstall = response.UpdatePackages;
        //        }
        //    }

        //    return packagesToInstall;
        //}

        private DirectoryInfo GetUpdatesSubfolder(IEnumerable<UpdatePackageData> packagesToDownload, string basePath)
        {
            return PathFinder.GetUpdatesSubfolder(basePath, this.UpdatesFolderName, packagesToDownload);
        }
    }
}