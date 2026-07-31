using System;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using Scopophobia.Dependencies;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Scopophobia
{
    [Serializable]
    public class Config : SyncedInstance<Config>
    {
        public static ConfigEntry<bool> ExtendedLoggingConfig;
        public static ConfigEntry<bool> AppearsConfig;
        public static ConfigEntry<bool> HasGlowingEyesConfig;
        public static ConfigEntry<string> SoundPackConfig;
        public static ConfigEntry<bool> BloodyTextureConfig;
        public static ConfigEntry<bool> DeathMakesBloodyConfig;
        public static ConfigEntry<float> SpeedDocileMultiplierConfig;
        public static ConfigEntry<float> SpeedRageMultiplierConfig;
        public static ConfigEntry<float> VolumeConfig;
        public static ConfigEntry<float> TriggerTimeConfig;
        public static ConfigEntry<float> FaceTriggerRangeConfig;
        public static ConfigEntry<float> FaceTriggerGracePeriodConfig;
        public static ConfigEntry<bool> HasMaxTargetsConfig;
        public static ConfigEntry<int> MaxTargetsConfig;
        public static ConfigEntry<bool> CanExitFacilityConfig;
        public static ConfigEntry<string> SpawnProbabilityCurveConfig;
        public static ConfigEntry<float> ShyGuyPowerLevelConfig;
        public static ConfigEntry<int> paintingSpawnRateConfig;
        public static ConfigEntry<bool> hidePaintingNameConfig;
        public static ConfigEntry<string> nameToUseForPaintingConfig;
        public static ConfigEntry<int> ChanceOfShyGuyConfig;
        public static ConfigEntry<bool> TwitchIntegrationConfig;
        public static ConfigEntry<bool> canBreakIntoShipConfig;
        public static ConfigEntry<bool> EnablePaintingConfig;
        public static bool ExtendedLogging;
        public static bool appears;

        public static bool hasGlowingEyes;
        public static string soundPack;

        public static bool bloodyTexture;

        public static bool deathMakesBloody;

        public static bool DisableSpawnRates;

        public static float speedDocileMultiplier;

        public static float speedRageMultiplier;

        public static float VolumeConfigs;

        public static float triggerTime;

        public static float faceTriggerRange;

        public static float faceTriggerGracePeriod;

        public static bool hasMaxTargets;

        public static int maxTargets;

        public static bool canExitFacility;

        public static string SpawnProbabilityCurve;

        public static float midEnemySpawnCurve;

        public static float endEnemySpawnCurve;

        public static bool spawnOutsideHardPlanets;
        public static bool twitchIntegration;

        public static float ShyGuyPowerLevel;
        
        public static int PaintingSpawnRate;
        public static bool hidePaintingName;
        public static string nameToUseForPainting;
        public static int ChanceOfShyGuy;
        public static bool canBreakIntoShip;
        public static bool EnablePainting;

        public static void SetModIcon(Sprite sprite)
        {
            if (LethalConfigProxy.Enabled)
            {
                LethalConfigProxy.SetModIcon(sprite);
            }
        }

        public static void SetModDescription(string description)
        {
            if (LethalConfigProxy.Enabled)
            {
                LethalConfigProxy.SetModDescription(description);
            }
        }

        public static void SkipAutoGen()
        {
            if (LethalConfigProxy.Enabled)
            {
                LethalConfigProxy.SkipAutoGen();
            }
        }
        public Config(ConfigFile cfg)
        {
            InitInstance(this);
            BindConfigs(cfg);
            SetupChangedEvents();
        }
        public void BindConfigs(ConfigFile cfg)
        {
            SkipAutoGen();
            AppearsConfig = Bind("General", "Enable the Shy Guy", defaultValue: true, requiresRestart: true, "Allows the Shy Guy to spawn in-game.");//used in ScopophobiaPlugin
            ExtendedLoggingConfig = Bind("General", "Enable Extended Logging", defaultValue: false, requiresRestart: false, "Enables Error and Warning Logs [Developer]");//as above
            SpeedDocileMultiplierConfig = Bind("General", "Speed Multiplier (Docile)", 1f, requiresRestart: false, "Determines the speed multiplier of the Shy Guy while docile.");
            SpeedRageMultiplierConfig = Bind("General", "Speed Multiplier (Rage)", 1f, requiresRestart: false, "Determines the speed multiplier of the Shy Guy while enraged.");
            VolumeConfig = Bind("General", "Enemy Volume", 5f, requiresRestart: false, "Determines the volume of the Shy Guy, and how loud he is. (Set this Anywhere between 0 and 10. Default: 5f, Old Default: 5f)");//Scopoplugin
            canBreakIntoShipConfig = Bind("General", "Can Break into Ship", true, requiresRestart: true, "Determines if Shy Guy can break into the Ship");
            
            HasGlowingEyesConfig = Bind("Appearance", "Glowing Eyes", defaultValue: true, requiresRestart: false, "Gives the Shy Guy glowing eyes similar to the Bracken/Flowerman.");
            BloodyTextureConfig = Bind("Appearance", "Bloody Texture", defaultValue: false, requiresRestart: false, "Gives the Shy Guy his bloodier, original texture from SCP: Containment Breach.");
            DeathMakesBloodyConfig = Bind("Appearance", "Bloody Death", defaultValue: true, requiresRestart: false, "Causes the Shy Guy's material to become bloody once getting his first kill. Useless if Bloody Texture is enabled lol");
            SoundPackConfig = Bind("Appearance", "Sound Pack (Curated, SCPCB, SCPCBOld, SecretLab)", "Curated", requiresRestart: false, "Determines the sounds the Shy Guy uses. (SOME MAY NOT SYNC WELL WITH TRIGGER TIME) (Curated = Default, curated for the Lethal Company experience) (SCPCB = SCP-096 sounds from SCP: Containment Breach) (SCPCBOld = Old alpha SCP-096 sounds from SCP: Containment Breach) (SecretLab = SCP-096 sounds from SCP: Secret Laboratory)");
            
            TriggerTimeConfig = Bind("Trigger Settings", "Trigger Time", 66.4f, requiresRestart: true, "Determines how long the Shy Guy must remain in the Triggered state to become fully enraged.");
            FaceTriggerRangeConfig = Bind("Trigger Settings", "Face Trigger Range", 17.5f, requiresRestart: true, "Determines the face's trigger radius.");
            FaceTriggerGracePeriodConfig = Bind("Trigger Settings", "Face Trigger Grace Period", 0.5f, requiresRestart: true, "Determines the grace period when you see the face of the Shy Guy before he becomes enraged.");
            HasMaxTargetsConfig = Bind("Trigger Settings", "Has Max Targets", defaultValue: false, requiresRestart: true, "Determines if the Shy Guy has a maximum amount of targets.");
            MaxTargetsConfig = Bind("Trigger Settings", "Max Targets", 32, requiresRestart: true, "Determines the max amount of targets the Shy Guy can have. (requires HasMaxTargets)");
            CanExitFacilityConfig = Bind("Trigger Settings", "Can Exit Facility", defaultValue: true, requiresRestart: false, "Determines if the Shy Guy can exit the facility and into the outdoors (and vice versa) to attack its target.");
            
            SpawnProbabilityCurveConfig = Bind("Spawn Settings", "ProbabilityCurve", defaultValue: "0.2, 1.0, 0.75", requiresRestart: false, $"Determines how likely Shy Guy is to spawn throughout the day. Accepts an array of floats with each entry separated by a comma."); 
            ShyGuyPowerLevelConfig = Bind("Spawn Settings", "Shy Guy Power Level", 3.0f, requiresRestart: false, "Default Power Level for the Shy Guy to take up per level. (Default: 3.0)");
            
            paintingSpawnRateConfig = Bind("Painting Spawn Settings", "Shy Guy Painting Spawn Rarity", 5, requiresRestart: true, "Default Spawn Rarity for the ShyGuyPainting (Default: 5)");
            hidePaintingNameConfig = Bind("Painting Spawn Settings", "Hide Painting Name before Interaction", true, requiresRestart: true, "Disguise the painting as a different Loot Item? (Default: True)");
            nameToUseForPaintingConfig = Bind("Painting Spawn Settings", "Custom Painting Name", "Painting",requiresRestart: true, "Customise the Scannode name for the item on the map! (Default: Painting");
            ChanceOfShyGuyConfig = Bind("Painting Spawn Settings", "Spawn Chance", 35, requiresRestart: true, "Customise the spawn chance of shy guy spawning from the painting. Higher values mean more likely, lower values mean less likely. (Set to 100 for guaranteed spawns");
            EnablePaintingConfig = Bind("Painting Spawn Settings", "Enable Painting", true, requiresRestart: true, "Enable or disable the painting spawning in the Scrap Pool.");
            
            appears = AppearsConfig.Value;
            ExtendedLogging = ExtendedLoggingConfig.Value;
            hasGlowingEyes = HasGlowingEyesConfig.Value;
            bloodyTexture = BloodyTextureConfig.Value;
            deathMakesBloody = DeathMakesBloodyConfig.Value;
            soundPack = SoundPackConfig.Value;
            speedDocileMultiplier = SpeedDocileMultiplierConfig.Value;
            speedRageMultiplier = SpeedRageMultiplierConfig.Value;
            VolumeConfigs = VolumeConfig.Value;
            triggerTime = TriggerTimeConfig.Value;
            faceTriggerRange = FaceTriggerRangeConfig.Value;
            faceTriggerGracePeriod = FaceTriggerGracePeriodConfig.Value;
            hasMaxTargets = HasMaxTargetsConfig.Value;
            maxTargets = MaxTargetsConfig.Value;
            canExitFacility = CanExitFacilityConfig.Value;
            SpawnProbabilityCurve = SpawnProbabilityCurveConfig.Value;
            ShyGuyPowerLevel = ShyGuyPowerLevelConfig.Value;
            PaintingSpawnRate = paintingSpawnRateConfig.Value;
            hidePaintingName = hidePaintingNameConfig.Value;
            nameToUseForPainting = nameToUseForPaintingConfig.Value;
            ChanceOfShyGuy = ChanceOfShyGuyConfig.Value;
            EnablePainting = EnablePaintingConfig.Value;
            canBreakIntoShip = canBreakIntoShipConfig.Value;
        }
        private void SetupChangedEvents()
        {
            SpawnProbabilityCurveConfig.SettingChanged += SpawnProbabilityCurve_SettingChanged;
        }

        private void SpawnProbabilityCurve_SettingChanged(object sender, System.EventArgs e)
        {
            EnemyHelper.SetProbabilityCurve(EnemyDataManager.EnemyName, Utils.ToFloatsArray(SpawnProbabilityCurveConfig.Value));
        }
        public static void RequestSync()
        {
            if (!SyncedInstance<Config>.IsClient)
            {
                return;
            }
            using FastBufferWriter stream = new FastBufferWriter(SyncedInstance<Config>.IntSize, Allocator.Temp);
            SyncedInstance<Config>.MessageManager.SendNamedMessage("Scopophobia_OnRequestConfigSync", 0uL, stream);
        }
        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!SyncedInstance<Config>.IsHost)
            {
                return;
            }
            ScopophobiaPlugin.logger.LogInfo($"Config sync request received from client: {clientId}");
            byte[] array = SyncedInstance<Config>.SerializeToBytes(SyncedInstance<Config>.Instance);
            int value = array.Length; 
            int fbwLength = FastBufferWriter.GetWriteSize(array) + IntSize;
            using FastBufferWriter stream = new FastBufferWriter(fbwLength, Allocator.Temp);
            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);
                SyncedInstance<Config>.MessageManager.SendNamedMessage("Scopophobia_OnReceiveConfigSync", clientId, stream);
            }
            catch (Exception e)
            {
                ScopophobiaPlugin.logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }
        public static ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, bool requiresRestart, string description, AcceptableValueBase acceptableValues = null, Action<T> settingChanged = null, ConfigFile configFile = null)
        {
            configFile ??= ScopophobiaPlugin.Instance.Config;

            var configEntry = acceptableValues == null
                ? configFile.Bind(section, key, defaultValue, description)
                : configFile.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));

            if (settingChanged != null)
            {
                configEntry.SettingChanged += (object sender, EventArgs e) => settingChanged?.Invoke(configEntry.Value);
            }

            if (Dependencies.LethalConfigProxy.Enabled)
            {
                if (acceptableValues == null)
                {
                    Dependencies.LethalConfigProxy.AddConfig(configEntry, requiresRestart);
                }
                else
                {
                    Dependencies.LethalConfigProxy.AddConfigSlider(configEntry, requiresRestart);
                }
            }

            return configEntry;
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(SyncedInstance<Config>.IntSize))
            {
                ScopophobiaPlugin.logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }
            reader.ReadValueSafe(out int length, default);
            if (!reader.TryBeginRead(length))
            {
                ScopophobiaPlugin.logger.LogError("Config sync error: Host could not sync.");
                return;
            }
            byte[] data = new byte[length];
            reader.ReadBytesSafe(ref data, length);
            SyncedInstance<Config>.SyncInstance(data);
            ScopophobiaPlugin.logger.LogInfo("Successfully synced config with host.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (SyncedInstance<Config>.IsHost)
            {
                SyncedInstance<Config>.MessageManager.RegisterNamedMessageHandler("Scopophobia_OnRequestConfigSync", OnRequestSync);
                SyncedInstance<Config>.Synced = true;
            }
            else
            {
                SyncedInstance<Config>.Synced = false;
                SyncedInstance<Config>.MessageManager.RegisterNamedMessageHandler("Scopophobia_OnReceiveConfigSync", OnReceiveSync);
                RequestSync();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            SyncedInstance<Config>.RevertSync();
        }
    }
}