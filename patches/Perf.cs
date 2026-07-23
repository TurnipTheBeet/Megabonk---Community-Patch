using System;
using BepInEx.Configuration;

namespace MegabonkCommunityPatch;

internal static class Perf
{
	internal readonly struct Span : IDisposable
	{
		internal Span(string label) { }
		public void Dispose() { }
	}

	internal static bool Enabled => false;

	internal static void SetEnabled(bool v) { }

	internal static void Init(ConfigFile cfg) { }

	internal static Span Measure(string label) => new Span(label);

	internal static void Hit(string label) { }

	internal static void EnterDepth(string label) { }

	internal static void ExitDepth(string label) { }

	internal static void Report() { }
}