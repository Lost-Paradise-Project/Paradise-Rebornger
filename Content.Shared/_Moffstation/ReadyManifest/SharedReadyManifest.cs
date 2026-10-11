using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.ReadyManifest;

/// <summary>
///     A message to send to the server when requesting a ready manifest.
///     ReadyManifestSystem will open an EUI that will be updated whenever
///     a player changes their ready status.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState(Dictionary<ProtoId<JobPrototype>, ReadyManifestJobCount> jobCounts) : EuiStateBase /// LP edit , int > ReadyManifestJobCount
{
    public readonly Dictionary<ProtoId<JobPrototype>, ReadyManifestJobCount> JobCounts = jobCounts; /// LP edit , int > ReadyManifestJobCount
}

/// LP edit start
/// <summary>
/// Dividing ready-made players by high and medium priority.
/// Разделение готовых игроков по высокому и среднему приоритету.
/// </summary>
/// <param name="High">Players with High priority on job</param>
/// <param name="Medium">Players with Medium priority on job</param>
[Serializable, NetSerializable]
public readonly record struct ReadyManifestJobCount(int High, int Medium);
/// LP edit end
