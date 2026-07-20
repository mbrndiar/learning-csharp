using System.Text.Json;

namespace ComparativeKv.Tests.Support;

internal static class StarterMilestoneFeedback
{
    public static int SelectedMilestone { get; } = ResolveSelectedMilestone();

    public static void ThrowIfActive(ProcessResult result, JsonElement envelope)
    {
        if (SelectedMilestone == 0)
        {
            return;
        }

        if (result.ExitCode != 1 ||
            envelope.GetProperty("ok").GetBoolean() ||
            !string.Equals(
                envelope.GetProperty("error").GetProperty("category").GetString(),
                "incomplete",
                StringComparison.Ordinal) ||
            !string.Equals(
                envelope.GetProperty("error").GetProperty("details").GetProperty("reason").GetString(),
                "starter_milestone",
                StringComparison.Ordinal))
        {
            throw new Xunit.Sdk.XunitException(
                $"COMPARATIVE_STARTER_MILESTONE_{SelectedMilestone}_INCOMPLETE: expected the starter's stable incomplete envelope before fixture assertions.");
        }

        throw new Xunit.Sdk.XunitException(
            $"COMPARATIVE_STARTER_MILESTONE_{SelectedMilestone}_INCOMPLETE: the matching shared fixture reached the untouched starter and received its stable incomplete diagnostic. Implement milestone {SelectedMilestone}, then rerun this command.");
    }

    private static int ResolveSelectedMilestone()
    {
#if COMPARATIVE_MILESTONE_1
        return 1;
#elif COMPARATIVE_MILESTONE_2
        return 2;
#elif COMPARATIVE_MILESTONE_3
        return 3;
#elif COMPARATIVE_MILESTONE_4
        return 4;
#elif COMPARATIVE_MILESTONE_5
        return 5;
#else
        return 0;
#endif
    }
}
