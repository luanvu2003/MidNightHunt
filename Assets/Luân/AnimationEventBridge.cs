using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    public void PlayFootstepSound()
    {
        SendMessageUpwards("PlayFootstepSound", SendMessageOptions.DontRequireReceiver);
    }
}