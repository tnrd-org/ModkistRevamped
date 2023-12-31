﻿using System.Windows.Controls;
using TNRD.Modkist.Factories.ViewModels;
using TNRD.Modkist.ViewModels.Controls.Details.Sidebar;

namespace TNRD.Modkist.Views.Controls.Details.Sidebar;

public partial class ModTags : UserControl
{
    public ModTags()
    {
        ViewModel = App.GetService<ModTagsViewModelFactory>().Create();
        DataContext = this;

        InitializeComponent();
    }

    public ModTagsViewModel ViewModel { get; set; }
}
