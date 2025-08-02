using Engine.Core.Logging;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class GameTerrain : Actor
{
    [Component] public TerrainMeshComponent Terrain;

    [OnInit]
    protected void OnInit()
    {
        Logger.Info("OnInit");
    }
}