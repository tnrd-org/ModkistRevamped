﻿using TNRD.Modkist.ViewModels.Controls;

namespace TNRD.Modkist.Factories.ViewModels;

public class ModListViewModelFactory : FactoryBase<ModListViewModel>
{
    public ModListViewModelFactory(IServiceProvider provider)
        : base(provider)
    {
    }
}
