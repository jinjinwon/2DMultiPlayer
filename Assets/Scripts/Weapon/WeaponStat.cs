using Unity.Netcode;
using UnityEngine;

public class WeaponStat : MonoBehaviour
{
    public WeaponType WeaponType;
    private int damage;
    private PlayerController Owner;
    public int Damage { get { return damage; } set { damage = value; } }


    private void Awake()
    {
        switch(WeaponType)
        {
            case WeaponType.Hammer:
                Damage = 30;
                break;
            case WeaponType.Sword:
                Damage = 20;
                break;
            case WeaponType.Scythe:
                Damage = 25;
                break;
        }
    }

    public void SetOwner(PlayerController playerController)
    {
        Owner= playerController;
    }

    public void CallFunction_SFX(string sfx)
    {
        SoundManager.Instance.PlaySFX(sfx);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root != this.transform.root)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                if (Owner == player)
                    return;

                player.playerStats.OnTakeDamage(damage);
            }
        }
    }
}
