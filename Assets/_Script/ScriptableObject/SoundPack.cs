using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundPack", menuName = "Scriptable Objects/SoundPack")]
public class SoundPack : ScriptableObject
{
    public List<SoundData> soundList;
}
