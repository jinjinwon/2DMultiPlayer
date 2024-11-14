using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;       // �߻�ü �ӵ�
    public float lifespan = 2f;     // �߻�ü ���� �ð�

    private Vector3 direction;
    public WeaponStat weaponStat;

    public void Initialize(Vector3 direction, PlayerController player)
    {
        this.direction = direction.normalized;
        weaponStat.SetOwner(player);
        RotateProjectile();
        Invoke("ReturnPool", lifespan);
    }

    void Update()
    {
        if (this.gameObject.activeSelf == true)
            transform.position += direction * speed * Time.deltaTime;
    }

    private void RotateProjectile()
    {
        if (direction != Vector3.zero)
        {
            // Z���� �������� ȸ��
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; 
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    private void ReturnPool()
    {
        if(this.gameObject.activeSelf == true)
            ObjectPool.Instance.GetReturnPoolServerRpc(this.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
    }   
}
