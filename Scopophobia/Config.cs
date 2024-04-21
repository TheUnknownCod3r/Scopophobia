using System;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib;
using Unity.Collections;
using Unity.Netcode;

namespace Scopophobia;

[Serializable]
public class Config : SyncedInstance<Config>
{
	public static ConfigEntry<bool> AppearsConfig;

	public static ConfigEntry<bool> HasGlowingEyesConfig;

	public static ConfigEntry<string> SoundPackConfig;

	public static ConfigEntry<bool> BloodyTextureConfig;

	public static ConfigEntry<bool> DeathMakesBloodyConfig;

	public static ConfigEntry<float> SpeedDocileMultiplierConfig;

	public static ConfigEntry<float> SpeedRageMultiplierConfig;

	public static ConfigEntry<int> SpawnRarityConfig;

	public static ConfigEntry<float> TriggerTimeConfig;

	public static ConfigEntry<float> FaceTriggerRangeConfig;

	public static ConfigEntry<float> FaceTriggerGracePeriodConfig;

	public static ConfigEntry<bool> HasMaxTargetsConfig;

	public static ConfigEntry<int> MaxTargetsConfig;

	public static ConfigEntry<bool> CanExitFacilityConfig;

	public static ConfigEntry<int> MaxSpawnCountConfig;

	public static ConfigEntry<bool> CanSpawnOutsideConfig;

	public static ConfigEntry<bool> SpawnOutsideHardPlanetsConfig;

	public static ConfigEntry<float> StartEnemySpawnCurveConfig;

	public static ConfigEntry<float> MidEnemySpawnCurveConfig;

	public static ConfigEntry<float> EndEnemySpawnCurveConfig;

	public static bool appears;

	public static bool hasGlowingEyes;

	public static string soundPack;

	public static bool bloodyTexture;

	public static bool deathMakesBloody;

	public static float speedDocileMultiplier;

	public static float speedRageMultiplier;

	public static int spawnRarity;

	public static float triggerTime;

	public static float faceTriggerRange;

	public static float faceTriggerGracePeriod;

	public static bool hasMaxTargets;

	public static int maxTargets;

	public static bool canExitFacility;

	public static bool canSpawnOutside;

	public static int maxSpawnCount;

	public static bool spawnsOutside;

	public static bool spawnOutsideHardPlanets;

	public static float startEnemySpawnCurve;

	public static float midEnemySpawnCurve;

	public static float endEnemySpawnCurve;

	public Config(ConfigFile cfg)
	{
		InitInstance(this);
		AppearsConfig = cfg.Bind("General", "Enable the Shy Guy", defaultValue: true, "Allows the Shy Guy to spawn in-game.");
		HasGlowingEyesConfig = cfg.Bind("Appearance", "Glowing Eyes", defaultValue: true, "Gives the Shy Guy glowing eyes similar to the Bracken/Flowerman.");
		BloodyTextureConfig = cfg.Bind("Appearance", "Bloody Texture", defaultValue: false, "Gives the Shy Guy his bloodier, original texture from SCP: Containment Breach.");
		DeathMakesBloodyConfig = cfg.Bind("Appearance", "Bloody Death", defaultValue: true, "Causes the Shy Guy's material to become bloody once getting his first kill. Useless if Bloody Texture is enabled lol");
		SoundPackConfig = cfg.Bind("Appearance", "Sound Pack (Curated, SCPCB, SCPCBOld, SecretLab)", "Curated", "Determines the sounds the Shy Guy uses. (SOME MAY NOT SYNC WELL WITH TRIGGER TIME) (Curated = Default, curated for the Lethal Company experience) (SCPCB = SCP-096 sounds from SCP: Containment Breach) (SCPCBOld = Old alpha SCP-096 sounds from SCP: Containment Breach) (SecretLab = SCP-096 sounds from SCP: Secret Laboratory)");
		SpeedDocileMultiplierConfig = cfg.Bind("Values", "Speed Multiplier (Docile)", 1f, "Determines the speed multiplier of the Shy Guy while docile.");
		SpeedRageMultiplierConfig = cfg.Bind("Values", "Speed Multiplier (Rage)", 1f, "Determines the speed multiplier of the Shy Guy while enraged.");
		SpawnRarityConfig = cfg.Bind("Values", "Spawn Rarity", 15, "Determines the spawn weight of the Shy Guy. Higher weights mean the Shy Guy is more likely appear. (just dont set this astronomically high)");
		TriggerTimeConfig = cfg.Bind("Values.Triggering", "Trigger Time", 66.4f, "Determines how long the Shy Guy must remain in the Triggered state to become fully enraged.");
		FaceTriggerRangeConfig = cfg.Bind("Values.Triggering", "Face Trigger Range", 17.5f, "Determines the face's trigger radius.");
		FaceTriggerGracePeriodConfig = cfg.Bind("Values.Triggering", "Face Trigger Grace Period", 0.5f, "Determines the grace period when you see the face of the Shy Guy before he becomes enraged.");
		HasMaxTargetsConfig = cfg.Bind("Values.Triggering", "Has Max Targets", defaultValue: false, "Determines if the Shy Guy has a maximum amount of targets.");
		MaxTargetsConfig = cfg.Bind("Values.Triggering", "Max Targets", 32, "Determines the max amount of targets the Shy Guy can have. (requires HasMaxTargets)");
		CanExitFacilityConfig = cfg.Bind("Values.Triggering", "Can Exit Facility", defaultValue: true, "Determines if the Shy Guy can exit the facility and into the outdoors (and vice versa) to attack its target.");
		MaxSpawnCountConfig = cfg.Bind("Values.Spawning", "Max Spawn Count", 1, "Determines how many Shy Guy can spawn total.");
		CanSpawnOutsideConfig = cfg.Bind("Values.Spawning", "Can Spawn Outside", defaultValue: true, "Determines if the Shy Guy can spawn outside. ");
		SpawnOutsideHardPlanetsConfig = cfg.Bind("Values.Spawning", "Spawn Outside Only Hard Moons", defaultValue: true, "If set to true, the Shy Guy will only spawn outside on the highest-difficulty/modded moons.");
		StartEnemySpawnCurveConfig = cfg.Bind("Values.Spawning", "Start Spawn Curve", 0.2f, "Spawn curve for the Shy Guy when the day starts. (typically 0-1)");
		MidEnemySpawnCurveConfig = cfg.Bind("Values.Spawning", "Midday Spawn Curve", 1f, "Spawn curve for the Shy Guy midday. (typically 0-1)");
		EndEnemySpawnCurveConfig = cfg.Bind("Values.Spawning", "End of Day Spawn Curve", 0.75f, "Spawn curve for the Shy Guy at the end of day. (typically 0-1)");
		appears = AppearsConfig.Value;
		hasGlowingEyes = HasGlowingEyesConfig.Value;
		bloodyTexture = BloodyTextureConfig.Value;
		deathMakesBloody = DeathMakesBloodyConfig.Value;
		soundPack = SoundPackConfig.Value;
		speedDocileMultiplier = SpeedDocileMultiplierConfig.Value;
		speedRageMultiplier = SpeedRageMultiplierConfig.Value;
		spawnRarity = SpawnRarityConfig.Value;
		triggerTime = TriggerTimeConfig.Value;
		faceTriggerRange = FaceTriggerRangeConfig.Value;
		faceTriggerGracePeriod = FaceTriggerGracePeriodConfig.Value;
		hasMaxTargets = HasMaxTargetsConfig.Value;
		maxTargets = MaxTargetsConfig.Value;
		canExitFacility = CanExitFacilityConfig.Value;
		maxSpawnCount = MaxSpawnCountConfig.Value;
		canSpawnOutside = CanSpawnOutsideConfig.Value;
		spawnOutsideHardPlanets = SpawnOutsideHardPlanetsConfig.Value;
		startEnemySpawnCurve = StartEnemySpawnCurveConfig.Value;
		midEnemySpawnCurve = MidEnemySpawnCurveConfig.Value;
		endEnemySpawnCurve = EndEnemySpawnCurveConfig.Value;
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
		Plugin.logger.LogInfo($"Config sync request received from client: {clientId}");
		byte[] array = SyncedInstance<Config>.SerializeToBytes(SyncedInstance<Config>.Instance);
		int value = array.Length;
		using FastBufferWriter stream = new FastBufferWriter(value + SyncedInstance<Config>.IntSize, Allocator.Temp);
		try
		{
			stream.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			stream.WriteBytesSafe(array);
			SyncedInstance<Config>.MessageManager.SendNamedMessage("Scopophobia_OnReceiveConfigSync", clientId, stream);
		}
		catch (Exception e)
		{
			Plugin.logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
		}
	}

	public static void OnReceiveSync(ulong _, FastBufferReader reader)
	{
		if (!reader.TryBeginRead(SyncedInstance<Config>.IntSize))
		{
			Plugin.logger.LogError("Config sync error: Could not begin reading buffer.");
			return;
		}
		reader.ReadValueSafe(out int val, default(FastBufferWriter.ForPrimitives));
		if (!reader.TryBeginRead(val))
		{
			Plugin.logger.LogError("Config sync error: Host could not sync.");
			return;
		}
		byte[] data = new byte[val];
		reader.ReadBytesSafe(ref data, val);
		SyncedInstance<Config>.SyncInstance(data);
		Plugin.logger.LogInfo("Successfully synced config with host.");
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
