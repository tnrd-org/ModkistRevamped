﻿using System.IO;
using Modio;
using Newtonsoft.Json;
using Wpf.Ui.Appearance;

namespace TNRD.Modkist.Services;

public partial class SettingsService : ObservableObject
{
    private const string FILE_PATH = "settings.json";

    [ObservableProperty] private string zeepkistDirectory = string.Empty;
    [ObservableProperty] private ApplicationTheme theme = ApplicationTheme.Unknown;
    [ObservableProperty] private AccessToken? accessToken;

    public SettingsService()
    {
        if (File.Exists(FILE_PATH))
        {
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText(FILE_PATH), this);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    partial void OnThemeChanged(ApplicationTheme value)
    {
        Save();
    }

    partial void OnZeepkistDirectoryChanged(string value)
    {
        Save();
    }

    partial void OnAccessTokenChanged(AccessToken? value)
    {
        Save();
    }

    private void Save()
    {
        File.WriteAllText(FILE_PATH, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public bool HasValidAccessToken()
    {
        if (AccessToken == null)
            return false;

        if (string.IsNullOrEmpty(AccessToken.Value))
            return false;

        if (!AccessToken.ExpiredAt.HasValue)
            return false;

        return DateTimeOffset.UtcNow < DateTimeOffset.FromUnixTimeSeconds(AccessToken.ExpiredAt!.Value);
    }
}
