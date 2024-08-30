using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LethalLib;
using Unity.Netcode;

[Serializable]
public class SyncedInstance<T>
{
    [NonSerialized]
    protected static int IntSize = 4;

    internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;

    internal static bool IsClient => NetworkManager.Singleton.IsClient;

    internal static bool IsHost => NetworkManager.Singleton.IsHost;

    public static T Default { get; private set; }

    public static T Instance { get; private set; }

    public static bool Synced { get; internal set; }

    protected void InitInstance(T instance)
    {
        Default = instance;
        Instance = instance;
        IntSize = 4;
    }

    internal static void SyncInstance(byte[] data)
    {
        Instance = DeserializeFromBytes(data);
        Synced = true;
    }

    internal static void RevertSync()
    {
        Instance = Default;
        Synced = false;
    }

    public static byte[] SerializeToBytes(T val)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using MemoryStream stream = new MemoryStream();
        try
        {
            bf.Serialize(stream, val);
            return stream.ToArray();
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Error serializing instance: {e}");
            return null;
        }
    }

    public static T DeserializeFromBytes(byte[] data)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using MemoryStream stream = new MemoryStream(data);
        try
        {
            return (T)bf.Deserialize(stream);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Error deserializing instance: {e}");
            return default(T);
        }
    }
}
