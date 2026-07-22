using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Steamworks;

namespace MegabonkCommunityPatch;

internal static class LeaderboardRelay
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.Leaderboard");

	internal static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8.0)
	};

	private static string Base => Plugin.LeaderboardServer.TrimEnd('/');

	internal static bool Enabled => !string.IsNullOrEmpty(Plugin.LeaderboardServer);

	internal static void SendBothBoards(int score, int character, int mapIndex)
	{
		Send("kills", score, character, mapIndex);
	}

	internal static void Send(string board, int score, int character, int mapIndex)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		if (!Enabled)
		{
			return;
		}
		if (ModGui.CheatsUsed)
		{
			Log.LogInfo((object)"[Leaderboard] Score blocked — F1 menu used this run.");
			return;
		}
		if (ModGui.UnauthorizedMods.Count > 0)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(49, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Leaderboard] Score blocked — unauthorized mods: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(string.Join(", ", ModGui.UnauthorizedMods));
			}
			Log.LogWarning(val);
			return;
		}
		string runData = RunDataCollector.Collect();
		Task.Run<Task>((Func<Task>)async delegate
		{
			bool flag2 = default(bool);
			try
			{
				ulong steamId = SteamUser.GetSteamID().m_SteamID;
				string name = SteamFriends.GetPersonaName();
				string payload = $"{{\"board\":\"{board}\",\"score\":{score},\"steamId\":\"{steamId}\",\"name\":{JsonSerializer.Serialize(name)},\"characterIndex\":{character},\"modVersion\":\"{Plugin.ModVersion}\",\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()},\"mapIndex\":{mapIndex},\"runData\":{JsonSerializer.Serialize(runData)}}}";
				StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
				await Http.PostAsync(Base + "/submit", content);
				BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(33, 4, out flag2);
				if (flag2)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Submitted score ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(score);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" char=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(character);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" on '");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(board);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("' as ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(name);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
				}
				Log.LogInfo(val2);
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out flag2);
				if (flag2)
				{
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Leaderboard server unreachable: ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
				}
				Log.LogWarning(val3);
			}
		});
	}

	internal static async Task<string> FetchPersonal(string steamId)
	{
		if (!Enabled)
		{
			return "";
		}
		try
		{
			return await Http.GetStringAsync(Base + "/personal?steamId=" + Uri.EscapeDataString(steamId));
		}
		catch
		{
			return "";
		}
	}

	internal static async Task<string> FetchEntries(string board, int count = 20, int characterIndex = -1, string group = null)
	{
		if (!Enabled)
		{
			return "";
		}
		try
		{
			string url = $"{Base}/entries?board={Uri.EscapeDataString(board)}&count={count}";
			if (characterIndex >= 0)
			{
				url += $"&characterIndex={characterIndex}";
			}
			if (!string.IsNullOrEmpty(group))
			{
				url = url + "&group=" + Uri.EscapeDataString(group);
			}
			return await Http.GetStringAsync(url);
		}
		catch
		{
			return "";
		}
	}
}
