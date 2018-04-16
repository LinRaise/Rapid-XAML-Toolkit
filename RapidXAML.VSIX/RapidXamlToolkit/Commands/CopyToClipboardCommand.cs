﻿// <copyright file="CopyToClipboardCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace RapidXamlToolkit
{
    internal sealed class CopyToClipboardCommand : GetXamlFromCodeWindowBaseCommand
    {
        public const int CommandId = 4128;

        public static readonly Guid CommandSet = new Guid("8c20aab1-50b0-4523-8d9d-24d512fa8154");

        private readonly AsyncPackage package;
        private readonly ILogger logger;

        private CopyToClipboardCommand(AsyncPackage package, OleMenuCommandService commandService, ILogger logger)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.logger = logger;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += this.MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static CopyToClipboardCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
        {
            // Verify the current thread is the UI thread - the call to AddCommand in CreateXamlStringCommand's constructor requires the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CopyToClipboardCommand(package, commandService, logger);

            AnalyzerBase.ServiceProvider = (IServiceProvider)Instance.ServiceProvider;
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuCmd)
            {
                menuCmd.Visible = menuCmd.Enabled = false;

                if (AnalyzerBase.GetSettings().IsActiveProfileSet)
                {
                    menuCmd.Visible = menuCmd.Enabled = true;
                }
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            var output = this.GetXaml(Instance.ServiceProvider);

            if (output != null && output.OutputType != AnalyzerOutputType.None)
            {
                var message = output.Output;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Clipboard.SetText(message);
                }
                else
                {
                    // Log no output
                }

                ShowStatusBarMessage(Instance.ServiceProvider, $"Copied XAML for {output.OutputType}: {output.Name}");
                this.logger.RecordInfo($"Copied XAML for {output.OutputType}: {output.Name}");
            }
            else
            {
                ShowStatusBarMessage(Instance.ServiceProvider, "No XAML copied.");
                this.logger.RecordInfo("No XAML copied.");
            }
        }
    }
}
