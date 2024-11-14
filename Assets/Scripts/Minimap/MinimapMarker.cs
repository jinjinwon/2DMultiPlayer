using UnityEngine;



public class MinimapMarker : MonoBehaviour
{
    public delegate void MiniMapUpdate(Vector3 position, MinimapType type, bool bActive, int index);

    public RectTransform playerIcon;
    public RectTransform[] enemyIcons;
    public RectTransform[] itemIcons;

    public MiniMapUpdate OnMiniMapUpdate;
    public RectTransform TraceTargetPos;

    public float minimapWidth = 100f; // �̴ϸ��� �ʺ�
    public float minimapHeight = 100f; // �̴ϸ��� ����
    public float worldWidth = 50f; // ���� ������ �ʺ�
    public float worldHeight = 50f; // ���� ������ ����

    private void Awake()
    {
        OnMiniMapUpdate += UpdateIconPosition;
    }

    private void UpdateIconPosition(Vector3 worldPosition, MinimapType type, bool bActive, int index = 0)
    {
        if (bActive == false)
        {
            GetRectTransform(type, index).gameObject.SetActive(false);
            return;
        }
        else
        {
            Vector3 minimapPosition = new Vector3(worldPosition.x, worldPosition.y, 0);

            // �̴ϸ� �߾��� (0,0)�� �� ��ġ ��ȯ
            minimapPosition.x = ((worldPosition.x + (worldWidth / 2)) / worldWidth) * minimapWidth - (minimapWidth / 2);
            minimapPosition.y = ((worldPosition.y + (worldHeight / 2)) / worldHeight) * minimapHeight - (minimapHeight / 2);

            // ��ġ ����
            minimapPosition.x -= minimapWidth / 2; // �̴ϸ� �߾����� �̵�
            minimapPosition.y -= minimapHeight / 2; // �̴ϸ� �߾����� �̵�

            GetRectTransform(type, index).gameObject.SetActive(true);
            GetRectTransform(type, index).anchoredPosition = minimapPosition;
        }
    }

    private RectTransform GetRectTransform(MinimapType type, int index = 0)
    {
        switch(type)
        {
            case MinimapType.Player:
                return playerIcon;
            case MinimapType.Enemy:
                if (index > 0 && index <= enemyIcons.Length)
                    return enemyIcons[index -1];
                break;
            case MinimapType.Item:
                if (index > 0 && index <= itemIcons.Length)
                    return itemIcons[index - 1];
                break;
        }
        return null;
    }
}
