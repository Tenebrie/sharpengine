﻿using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Assets.Meshes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class UnitCube : Actor
{
    [Component]
    public InstancedActorComponent<UnitCubeInstance> InstanceManager;
    
    [OnInit]
    protected void OnInit()
    {
        InstanceManager.Mesh = new StaticMesh();
        InstanceManager.Material = new HonseTerrainMaterial();
        InstanceManager.Mesh.LoadUnitCube();
        // ObjMeshLoader.LoadObj("bin/decimated_dragon32.obj", out AssetVertex[] vertices, out var indices);
        // InstanceManager.Mesh.Load(vertices, indices);
    } 
}

public class UnitCubeInstance : ActorInstance
{
    // TODO: Performance fix on large number of ticking instances?
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
    //     // Transform.Translate(-0.5 * deltaTime, 0, 0);
        Transform.Rotate(3 * deltaTime, 5 * deltaTime, 7 * deltaTime);
    }
}