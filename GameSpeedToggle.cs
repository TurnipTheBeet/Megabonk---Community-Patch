#nullable disable
using UnityEngine;

namespace MegaBonkMod;

// Toggles game speed between 1x and 2x by scaling Time.timeScale. Counts as a
// cheat (inflates kills/time per real second), so flips CheatsUsed → leaderboard
// submissions are blocked, same as God Mode / Instakill / spawn cheats.
//
// We only override timeScale when the game is actually running (timeScale > 0).
// While paused the game holds timeScale at 0; we leave that alone and re-apply
// our 2x once it resumes (handled in Tick).
internal static class GameSpeedToggle
{
    const float FastMult = 2f;

    internal static bool Fast { get; private set; }

    internal static void Toggle()
    {
        Fast = !Fast;
        if (Fast)
        {
            ModGui.CheatsUsed = true;
            if (Time.timeScale > 0f) Time.timeScale = FastMult;
            Toast.Show("Game Speed: 2x", Color.cyan);
        }
        else
        {
            if (Time.timeScale > 0f) Time.timeScale = 1f;
            Toast.Show("Game Speed: 1x", Color.gray);
        }
    }

    // Re-apply 2x after the game un-pauses (pause forces timeScale to 0, then back
    // to 1 on resume — without this our speed would silently reset).
    internal static void Tick()
    {
        if (!Fast) return;
        if (Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, FastMult))
            Time.timeScale = FastMult;
    }
}
