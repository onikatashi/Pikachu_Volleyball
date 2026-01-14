using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    public SoundData sound;

    void Start()
    {
        SoundManager.Instance.PlayBGM(sound.name);
    }

}
