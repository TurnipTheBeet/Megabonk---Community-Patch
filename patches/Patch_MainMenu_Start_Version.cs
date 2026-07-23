using System;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MainMenu), "Start")]
internal static class Patch_MainMenu_Start_Version
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.Version");

	[HarmonyPostfix]
	private static void Postfix()
	{
		ModGui.MainThread.Enqueue(delegate
		{
			ModGui.NeedVersionPatch = true;
			ModGui.ResetVersionScan();
		});
		CheckVersionAsync();
		PersonalTab.BeginFetch();
		CheckUnauthorizedMods();
	}

	private static void CheckUnauthorizedMods()
	{
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		ModGui.UnauthorizedMods.Clear();
		bool flag = default(bool);
		try
		{
			IL2CPPChainloader instance = IL2CPPChainloader.Instance;
			PropertyInfo property = ((object)instance).GetType().GetProperty("Plugins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null)
			{
				Log.LogError((object)"[ModCheck] Plugins property not found.");
				return;
			}
			if (property.GetValue(instance) is IDictionary dictionary)
			{
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(29, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[ModCheck] ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(dictionary.Count);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" plugin(s) loaded.");
				}
				Log.LogInfo(val);
				foreach (object key in dictionary.Keys)
				{
					if (!(key.ToString() == "com.megabonk.mod"))
					{
						BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[ModCheck] Unauthorized plugin: ");
							((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<object>(key);
						}
						Log.LogWarning(val2);
						ModGui.UnauthorizedMods.Add(key.ToString());
					}
				}
			}
			else
			{
				Log.LogError((object)"[ModCheck] Plugins cast failed.");
			}
		}
		catch (Exception ex)
		{
			BepInExErrorLogInterpolatedStringHandler val3 = new BepInExErrorLogInterpolatedStringHandler(19, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[ModCheck] Failed: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
			}
			Log.LogError(val3);
		}
		if (ModGui.UnauthorizedMods.Count == 0)
		{
			Log.LogInfo((object)"[ModCheck] No unauthorized mods detected.");
		}
	}

	private static void CheckVersionAsync()
	{
		Task.Run<Task>((Func<Task>)async delegate
		{
			bool flag = default(bool);
			try
			{
				using JsonDocument doc = JsonDocument.Parse(await LeaderboardRelay.Http.GetStringAsync(Plugin.LeaderboardServer.TrimEnd('/') + "/version"));
				if (doc.RootElement.TryGetProperty("required", out var req))
				{
					string required = req.GetString() ?? "";
						if (!string.IsNullOrEmpty(required) && Version.TryParse(required, out Version reqVer) && Version.TryParse(Plugin.ModVersion, out Version localVer) && reqVer > localVer)
					{
						BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(30, 2, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Mod outdated: local=");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("1.4.4");
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" required=");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(required);
						}
						Log.LogWarning(val);
						ModGui.MainThread.Enqueue(delegate
						{
							ModGui.UpdateAvailable = true;
						});
					}
					else
					{
						BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(17, 1, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Mod version OK (");
							((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>("1.4.4");
							((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(")");
						}
						Log.LogInfo(val2);
					}
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(22, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Version check failed: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
				}
				Log.LogWarning(val);
			}
		});
	}
}
