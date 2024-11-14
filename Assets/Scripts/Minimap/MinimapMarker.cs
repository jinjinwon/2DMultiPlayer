using UnityEngine;



public class MinimapMarker : MonoBehaviour
{
    public delegate void MiniMapUpdate(Vector3 position, MinimapType type, bool bActive, int index);

    public RectTransform playerIcon;
    public RectTransform[] enemyIcons;
    public RectTransform[] itemIcons;

    public MiniMapUpdate OnMiniMapUpdate;
    public RectTransform TraceTargetPos;

    public float minimapWidth = 100f; // 미니맵의 너비
    public float minimapHeight = 100f; // 미니맵의 높이
    public float worldWidth = 50f; // 게임 월드의 너비
    public float worldHeight = 50f; // 게임 월드의 높이

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

            // 미니맵 중앙이 (0,0)일 때 위치 변환
            minimapPosition.x = ((worldPosition.x + (worldWidth / 2)) / worldWidth) * minimapWidth - (minimapWidth / 2);
            minimapPosition.y = ((worldPosition.y + (worldHeight / 2)) / worldHeight) * minimapHeight - (minimapHeight / 2);

            // 위치 조정
            minimapPosition.x -= minimapWidth / 2; // 미니맵 중앙으로 이동
            minimapPosition.y -= minimapHeight / 2; // 미니맵 중앙으로 이동

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
