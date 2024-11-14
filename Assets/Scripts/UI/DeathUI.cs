using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class DeathUI : MonoBehaviour
{
    public CinemachineCamera virtualCamera;
    public GameObject[] aliveObjects;
    public List<GameObject> users = new List<GameObject>();
    private int iNum = 0;

    private void OnEnable()
    {
        for (int i = 0; i < aliveObjects.Length; i++)
            aliveObjects[i].SetActive(false);
    }

    public void OnDeath()
    {
        this.gameObject.SetActive(true);
        FindOtherPlayers();
        SetCameraTarget(users.First().transform);
    }

    private void FindOtherPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player.gameObject.activeSelf == true)
            {
                users.Add(player);
            }
        }
    }

    private void SetCameraTarget(Transform newTarget)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = newTarget;
            virtualCamera.LookAt = newTarget;
        }
    }

    public void OnClickPrev()
    {
        Check();

        iNum--;
        if (iNum < 0)
        {
            iNum = users.Count - 1;
        }

        if (users.Count > 0)
        {
            SetCameraTarget(users[iNum].transform);
        }
    }

    public void OnClickNext()
    {
        Check();

        iNum++;
        if (iNum >= users.Count)
        {
            iNum = 0;
        }

        if (users.Count > 0)
        {
            SetCameraTarget(users[iNum].transform);
        }
    }

    private void Check()
    {
        users.RemoveAll(user => !user.activeSelf);
        if (iNum >= users.Count)
        {
            iNum = users.Count - 1;
        }
    }

    public void OnAlive()
    {
        this.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        for (int i = 0; i < aliveObjects.Length; i++)
            aliveObjects[i].SetActive(true);
    }
}
