using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class GameManage : MonoSingleton<GameManage>
{
    public CinemachineCamera Camera;
    public Animation victoryObj;
    public List<PlayerController> users = new List<PlayerController>();
    private bool gameEnd;


    public bool GameEnd
    {
        get
        {
            return gameEnd;
        }
        set
        {
            if (gameEnd == value)
                return;

            gameEnd = value;

            if(gameEnd == true)
            {
                ActivateZoomForActiveUsers();
            }
        }
    }

    public void GameCheck()
    {
        int iTemp = 0;
        bool bGameEnd = false;
        for(int i = 0; i < users.Count; i++)
        {
            if (!users[i].playerStats.Death)
            {
                Debug.Log($"Alive User Count {iTemp}");
                iTemp++;
            }
        }

        if (iTemp == 1)
        {
            bGameEnd = true;
        }

        GameEnd = bGameEnd;
    }


    // ������ �ڱ� �ڽ��� ���
    public void RegisterPlayer(PlayerController player)
    {
        users.Add(player);
    }

    // Ȱ��ȭ�� �����鿡�� ���� Ȱ��ȭ
    private void ActivateZoomForActiveUsers()
    {
        foreach (PlayerController user in users)
        {
            if (user.gameObject.activeSelf)
            {
                StartCoroutine(SmoothZoom(4.5f, 2f, 1f));
                break;
            }
        }
    }
    private IEnumerator SmoothZoom(float startSize, float endSize, float duration)
    {
        float elapsedTime = 0f;
        float initialSize = Camera.Lens.OrthographicSize;

        while (elapsedTime < duration)
        {
            Camera.Lens.OrthographicSize = Mathf.Lerp(startSize, endSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ���� ��ǥ ũ��� ����
        Camera.Lens.OrthographicSize = endSize;
        victoryObj.gameObject.SetActive(true);
        victoryObj.Play();
    }


    public void OnClickSceneChanged()
    {
        SceneTransitionManager.Instance.RequestTransitionToLobbyScene();
    }
}
