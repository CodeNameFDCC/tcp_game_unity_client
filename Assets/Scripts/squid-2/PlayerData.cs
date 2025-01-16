// PlayerData.cs


using UnityEngine;

public class PlayerSpawnData
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
}

public class PlayerDespawnData
{
    public int PlayerId { get; set; }
}

public class PlayerPositionData
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
}

public class PlayerRotationData
{
    public int PlayerId { get; set; }
    public Quaternion Rotation { get; set; }
}