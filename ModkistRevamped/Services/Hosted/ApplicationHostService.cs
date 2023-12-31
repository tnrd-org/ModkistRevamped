﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TNRD.Modkist.Views.Pages;
using TNRD.Modkist.Views.Windows;
using Wpf.Ui;

namespace TNRD.Modkist.Services.Hosted;

/// <summary>
/// Managed host of the application.
/// </summary>
public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates main window during activation.
    /// </summary>
    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            MainWindow? navigationWindow = _serviceProvider.GetRequiredService<MainWindow>();
            navigationWindow.Loaded += OnNavigationWindowLoaded;
            navigationWindow.Show();
        }
    }

    private async void OnNavigationWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow navigationWindow)
        {
            return;
        }

        Process[] processes = Process.GetProcessesByName("Zeepkist");
        if (processes.Length > 0)
        {
            IContentDialogService contentDialogService =
                _serviceProvider.GetRequiredService<IContentDialogService>();
            await contentDialogService.ShowAlertAsync("Uh oh",
                "Please close Zeepkist before you start Modkist",
                "OK");

            Application.Current.Shutdown();
            return;
        }

        navigationWindow.NavigationView.Navigate(typeof(InitializationPage));
    }
}
