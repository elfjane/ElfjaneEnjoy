using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Bloody.Core;
using Bloody.Core.API.v1;
using ElfjaneEnjoy.AutoAnnouncer;
using ElfjaneEnjoy.DB;
using HarmonyLib;
using System.IO;
using Unity.Entities;
using VampireCommandFramework;

namespace ElfjaneEnjoy;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("trodi.Bloody.Core")]
public class Plugin : BasePlugin
{
    Harmony _harmony;
    public static Bloody.Core.Helper.v1.Logger Logger;
    public static SystemsCore SystemsCore;

    public static ConfigEntry<bool> AutoAnnouncerConfig;
    public static ConfigEntry<int> IntervalAutoAnnouncer;
    public static ConfigEntry<bool> MiniGameConfig;
    public static ConfigEntry<int> IntervalMiniGame;
    public static ConfigEntry<int> MiniGameRewardItem;
    public static ConfigEntry<int> MiniGameRewardAmount;

    public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "ElfjaneEnjoy");

    public override void Load()
    {
        Logger = new(Log);

        if (!Core.IsServer)
        {
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is only for server!");
            return;
        }

        // Harmony patching
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        // Register all commands in the assembly with VCF
        CommandRegistry.RegisterAll();

        EventsHandlerSystem.OnInitialize += GameDataOnInitialize;
        InitConfig();
        LoadDatabase.LoadAllConfig();


        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");
    }

    private void GameDataOnInitialize(World world)
    {
        SystemsCore = Core.SystemsCore;

        Database.EnabledFeatures[NotifyFeature.auto] = AutoAnnouncerConfig.Value;
        Database.setIntervalAutoAnnouncer(IntervalAutoAnnouncer.Value);

        Database.EnabledFeatures[NotifyFeature.minigame] = MiniGameConfig.Value;
        Database.setIntervalMiniGame(IntervalMiniGame.Value);
        Database.setMiniGameReward(MiniGameRewardItem.Value, MiniGameRewardAmount.Value);

        AutoAnnouncerFunction.StartAutoAnnouncer();
        MiniGame.MiniGameFunction.StartMiniGame();

    }

    private void InitConfig()
    {
        AutoAnnouncerConfig = Config.Bind("AutoAnnouncer", "enabled", false, "Enable AutoAnnouncer.");
        IntervalAutoAnnouncer = Config.Bind("AutoAnnouncer", "interval", 300, "Interval seconds for spam AutoAnnouncer.");
        MiniGameConfig = Config.Bind("MiniGame", "enabled", false, "Enable MiniGame.");
        IntervalMiniGame = Config.Bind("MiniGame", "interval", 30, "Interval seconds between mini game starts.");
        MiniGameRewardItem = Config.Bind("MiniGame", "reward_item", 0, "Item prefab name to reward the winner (leave blank for announce only).");
        MiniGameRewardAmount = Config.Bind("MiniGame", "reward_amount", 1, "Amount of item to give as reward.");

        if (!Directory.Exists(ConfigPath)) Directory.CreateDirectory(ConfigPath);

        DB.Config.CheckAndCreateConfigs();


    }

    public void OnGameInitialized()
    {

    }

    public override bool Unload()
    {
        CommandRegistry.UnregisterAssembly();
        _harmony?.UnpatchSelf();
        return true;
    }

}
