using UnityEngine;

public class WeaponSelectPanel : MonoBehaviour
{
    public void OnClickWeapon(int type)
    {
        UIManager.Instance.OnWepaonChange((WeaponType)type);
        this.gameObject.SetActive(false);
    }
}
