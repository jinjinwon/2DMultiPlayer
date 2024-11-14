using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;       // 발사체 속도
    public float lifespan = 2f;     // 발사체 생명 시간

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
            // Z축을 기준으로 회전
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
