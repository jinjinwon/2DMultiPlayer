using UnityEngine;
using Unity.Netcode.Components;

public class PlayerAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
