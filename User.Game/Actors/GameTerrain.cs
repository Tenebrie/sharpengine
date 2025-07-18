using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class GameTerrain : Actor
{
    [Component] public TerrainMeshComponent Terrain;

    [OnInit]
    protected void OnInit()
    {
        Console.WriteLine("OnInit");
    }
}