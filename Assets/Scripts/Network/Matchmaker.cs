using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Matchmaker : NetworkBehaviour
{
    public int maxPlayers = 4;
    public int minPlayers = 2;

    [Header("LobbyZone")]
    public GameObject LobbyZone;
    public GameObject panel_Join;
    public GameObject panel_Create;

    [Header("SerachZone")]
    public GameObject SearchZone;


    [Header("RoomZone")]
    public GameObject RoomZone;
    public GameObject content;
    public Button button_Leave;

    #region Public Function
    public void ActiveSet(GameObject go, bool state) => go.SetActive(state);

    public void OnJoinedAction()
    {
        ActiveSet(LobbyZone, false);
        ActiveSet(SearchZone, false);
        ActiveSet(panel_Join, false);
        ActiveSet(panel_Create, false);
        ActiveSet(RoomZone, true);
    }

    public void OnLevedAction()
    {
        ActiveSet(LobbyZone, true);
        ActiveSet(SearchZone, false);
        ActiveSet(panel_Join, false);
        ActiveSet(panel_Create, false);
        ActiveSet(RoomZone, false);
    }

    public void OnMessage(string str)
    {
        Debug.Log(str);
    }
    #endregion


    #region LobbyZone Function
    public void OnClickJoin(bool state)
    {
        ActiveSet(panel_Join, state);
    }

    public void OnClickCreate(bool state)
    {
        ActiveSet(panel_Create, state);
    }

    public void OnClickSearchSession()
    {
        ActiveSet(LobbyZone, false);
        ActiveSet(SearchZone, true);
    }
    #endregion

    #region SearchZone Function
    public void OnClickSearchZoneExit()
    {
        ActiveSet(LobbyZone, true);
        ActiveSet(SearchZone, false);
    }
    #endregion

    #region RoomZone Function
    
    private IEnumerator GetActiveChildCount()
    {
        int activeChildCount = 0;

        foreach (Transform child in content.transform)
        {
            if (child.gameObject.activeSelf) // 자식이 활성화된 경우
            {
                activeChildCount++;
            }
        }

        SceneTransitionManager.Instance.expectedClientCount = activeChildCount;

        yield return null;
    }

    public void OnClickLeave()
    {
        button_Leave.onClick.Invoke();
    }

    public void OnClickPlay()
    {
        if (IsHost)
        {
            OnClickChildCountServerRpc();
        }
    }

    [ServerRpc]
    public void OnClickChildCountServerRpc()
    {
        OnClickChildCountClientRpc();
    }

    [ClientRpc]
    public void OnClickChildCountClientRpc()
    {
        StartCoroutine(TransitionScene());
    }

    public IEnumerator TransitionScene()
    {
        yield return GetActiveChildCount();
        SceneTransitionManager.Instance.TransitionToEmptyScene();
    }
    #endregion
}
