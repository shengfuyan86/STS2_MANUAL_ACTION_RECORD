using Godot;
using MegaCrit.Sts2.Core.Modding;
using STS2ManualActionRecorder.Integration;
using STS2ManualActionRecorder.Recording;

namespace STS2ManualActionRecorder;

[ModInitializer(nameof(Initialize))]
public partial class ModEntry : Node
{
    public static void Initialize()
    {
        RecorderSession? session = null;
        try
        {
            session = RecorderSession.Create();
            RecorderRuntime.Initialize(session);
            string harmonyId = HarmonyBootstrap.Initialize();
            session.Record(
                "recorder_loaded",
                "mod_initializer",
                "safe",
                new Dictionary<string, object?>
                {
                    ["harmony_id"] = harmonyId,
                    ["patches_applied"] = false,
                    ["diagnostic_hooks_applied"] = true,
                    ["diagnostic_hooks"] = new[]
                    {
                        "NEndTurnButton.CallReleaseLogic",
                        "NEndTurnButton.SecretEndTurnLogicViaFtue",
                        "CombatManager.AfterAllPlayersReadyToEndTurn"
                    },
                    ["output_directory"] = session.RunDirectory
                });
        }
        catch (Exception ex)
        {
            session?.RecordError(ex);
        }
    }
}
