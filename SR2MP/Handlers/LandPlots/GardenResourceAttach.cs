using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.ResourceAttach)]
public sealed class ResourceAttachHandler : BasePacketHandler<ResourceAttachPacket>
{
    protected override bool Handle(ResourceAttachPacket packet, IPEndPoint? _)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return false;

        var actor = model.Cast<ActorModel>();

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return false;

        var cycle = networkComponent.GetComponent<ResourceCycle>();
        if (cycle == null)
            return false;

        Joint? targetJoint = null;
        SpawnResource? currentGarden = null;

        if (!string.IsNullOrEmpty(packet.PlotID))
        {
            if (GameState.landPlots.TryGetValue(packet.PlotID, out var plotModel)
                && plotModel.gameObj)
            {
                currentGarden = plotModel.gameObj.GetComponentInChildren<SpawnResource>();
                if (currentGarden != null && packet.Joint >= 0 && packet.Joint < currentGarden.SpawnJoints.Count)
                    targetJoint = currentGarden.SpawnJoints[packet.Joint];

                if (currentGarden?._model != null)
                {
                    currentGarden._model.nextSpawnRipens = packet.Model.nextSpawnRipens;
                    currentGarden._model.nextSpawnTime = packet.Model.nextSpawnTime;
                    currentGarden._model.storedWater = packet.Model.storedWater;
                    currentGarden._model.wasPreviouslyPlanted = packet.Model.wasPreviouslyPlanted;
                }
            }
        }

        if (targetJoint == null)
        {
            foreach (var spawner in Object.FindObjectsOfType<SpawnResource>())
            {
                if (Vector3.Distance(spawner.transform.position, packet.SpawnerID) < 0.1f)
                {
                    if (packet.Joint >= 0 && packet.Joint < spawner.SpawnJoints.Count)
                        targetJoint = spawner.SpawnJoints[packet.Joint];

                    if (spawner._model != null)
                    {
                        spawner._model.nextSpawnRipens = packet.Model.nextSpawnRipens;
                        spawner._model.nextSpawnTime = packet.Model.nextSpawnTime;
                        spawner._model.storedWater = packet.Model.storedWater;
                        spawner._model.wasPreviouslyPlanted = packet.Model.wasPreviouslyPlanted;
                    }

                    break;
                }
            }
        }

        if (targetJoint == null)
            return false;

        handlingPacket = true;
        cycle.Attach(targetJoint);
        handlingPacket = false;

        currentGarden!._spawned.Add(cycle.gameObject);
        
        var produceModel = model.TryCast<ProduceModel>();
        if (produceModel != null)
            networkComponent.SetResourceState(produceModel._state, produceModel.progressTime, force: true);
        
        return true;
    }
}