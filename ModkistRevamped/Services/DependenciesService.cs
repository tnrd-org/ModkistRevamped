﻿using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modio;
using Modio.Models;
using Newtonsoft.Json;
using TNRD.Modkist.Models;
using TNRD.Modkist.Services.Subscription;
using File = System.IO.File;

namespace TNRD.Modkist.Services;

public class DependenciesService
{
    private readonly Dictionary<uint, HashSet<uint>> dependencyIdToModIds = new();
    private readonly ModsClient modsClient;
    private readonly ModCachingService modCachingService;
    private readonly ISubscriptionService subscriptionService;
    private readonly ILogger<DependenciesService> logger;

    public DependenciesService(
        ModsClient modsClient,
        ModCachingService modCachingService,
        ISubscriptionService subscriptionService,
        ILogger<DependenciesService> logger
    )
    {
        this.modsClient = modsClient;
        this.modCachingService = modCachingService;
        this.subscriptionService = subscriptionService;
        this.logger = logger;
    }

    public async Task Initialize()
    {
        dependencyIdToModIds.Clear();

        List<uint> modsToCheck = new();
        List<DependencyModel> dependencyModels = LoadDependenciesFromDisk();

        foreach (Mod mod in modCachingService)
        {
            DependencyModel? dependency = dependencyModels.FirstOrDefault(x => x.ModId == mod.Id);

            if (dependency == null)
            {
                modsToCheck.Add(mod.Id);
            }
            else if (mod.DateUpdated > dependency.Stamp)
            {
                modsToCheck.Add(mod.Id);
            }
            else
            {
                dependencyIdToModIds.Add(mod.Id, dependency.Dependencies);
            }
        }

        foreach (uint modId in modsToCheck)
        {
            IReadOnlyList<Dependency> dependencies = await modsClient[modId].Dependencies.Get();

            HashSet<uint> dependencyIds = new();
            foreach (Dependency dependency in dependencies)
            {
                dependencyIds.Add(dependency.ModId);
            }

            dependencyIdToModIds.Add(modId, dependencyIds);
        }

        SaveDependenciesToDisk();
    }

    public bool IsDependency(Mod mod)
    {
        return IsDependency(mod.Id);
    }

    public bool IsDependency(uint modId)
    {
        IEnumerable<KeyValuePair<uint, HashSet<uint>>> kvps = dependencyIdToModIds.Where(x => x.Value.Contains(modId));
        return kvps.Any(kvp => subscriptionService.IsSubscribed(kvp.Key));
    }

    public List<Mod> GetDependingMods(Mod mod)
    {
        return GetDependingMods(mod.Id);
    }

    public List<Mod> GetDependingMods(uint modId)
    {
        List<uint> dependingModIds = new();

        foreach (KeyValuePair<uint, HashSet<uint>> kvp in dependencyIdToModIds)
        {
            if (kvp.Value.Contains(modId))
            {
                dependingModIds.Add(kvp.Key);
            }
        }

        return dependingModIds.Distinct().Select(x => modCachingService[x]).ToList();
    }

    public List<Mod> GetDependencies(Mod mod)
    {
        return GetDependencies(mod.Id);
    }

    public List<Mod> GetDependencies(uint modId)
    {
        if (!dependencyIdToModIds.TryGetValue(modId, out HashSet<uint>? dependencyIds))
        {
            return new List<Mod>();
        }

        List<Mod> dependencies = new();

        foreach (uint dependencyId in dependencyIds)
        {
            if (!modCachingService.TryGetMod(dependencyId, out Mod? mod))
            {
                logger.LogWarning("Mod {ModId} has a dependency {DependencyId} but it could not be found in the cache",
                    modId,
                    dependencyId);

                if (modCachingService.TryGetMod(modId, out Mod? parentMod))
                {
                    logger.LogWarning("Follow-up: Parent mod name={Name} and url={Url}",
                        parentMod.Name,
                        parentMod.HomepageUrl);
                }
                else
                {
                    logger.LogWarning("Follow-up: Parent mod could not be found in the cache either!");
                }

                continue;
            }

            dependencies.Add(mod);
        }

        return dependencies;
    }

    public IReadOnlyList<uint> GetAllDependencies()
    {
        return dependencyIdToModIds.Keys.ToList();
    }

    private static string GetDependenciesPath()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Modkist",
            version == null
                ? "dependencies.json"
                : $"dependencies-v{version.ToString().Replace(".", string.Empty)}.json");
    }

    private List<DependencyModel> LoadDependenciesFromDisk()
    {
        string dependenciesPath = GetDependenciesPath();

        if (!File.Exists(dependenciesPath))
        {
            return JsonConvert.DeserializeObject<List<DependencyModel>>(PreloadedDependencies.Value)!;
        }

        string json = File.ReadAllText(dependenciesPath);
        return JsonConvert.DeserializeObject<List<DependencyModel>>(json)!;
    }

    private void SaveDependenciesToDisk()
    {
        string dependenciesPath = GetDependenciesPath();

        List<DependencyModel> dependencyModels = dependencyIdToModIds
            .Select(x => new DependencyModel(x.Key, modCachingService[x.Key].DateUpdated, x.Value)).ToList();
        string json = JsonConvert.SerializeObject(dependencyModels, Formatting.Indented);
        File.WriteAllText(dependenciesPath, json);
    }
}
