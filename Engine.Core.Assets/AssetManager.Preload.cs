using System.Reflection;

namespace Engine.Core.Assets;

public class PreparedAssetManager
{
    private readonly Dictionary<string, PreparedAsset> _preparedAssets = new(); 

    private static string GetKey(Type ownerClassType, int index)
    {
        return $"{ownerClassType.FullName}_{index}";
    }
    
    public void Save(Type ownerClassType, int index, object userData)
    {
        var asset = new PreparedAsset
        {
            OwnerClassType = ownerClassType,
            SourceHash = HashOf(ownerClassType),
            UserData = userData
        };
        _preparedAssets[GetKey(ownerClassType, index)] = asset;
    }

    public TAsset Load<TAsset>(Type ownerClassType, int index)
    {
        var key = GetKey(ownerClassType, index);
        if (_preparedAssets.TryGetValue(key, out var asset))
            return (TAsset)asset.UserData;
        throw new KeyNotFoundException($"Prepared asset for {ownerClassType.FullName} at index {index} not found.");
    }
    
    public bool Has(Type ownerClassType, int index)
    {
        return _preparedAssets.ContainsKey(GetKey(ownerClassType, index)) 
               && _preparedAssets[GetKey(ownerClassType, index)].SourceHash == HashOf(ownerClassType);
    }

    private static string HashOf(Type t)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        var field = t.GetField("__SourceHash", flags)
                    ?? throw new InvalidOperationException(
                        $"{t.FullName} is not marked with [OnPrepareResources].");

        return (string)field.GetRawConstantValue()!;
    }
}

public struct PreparedAsset
{
    public Type OwnerClassType;
    public string SourceHash;
    public object UserData;
}