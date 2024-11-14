using UnityEngine;

public class TipPanel : MonoBehaviour
{
    public GameObject FirstPanel;
    public GameObject SecondPanel;

    public void OnEnable()
    {
        OnClickPage(1);
    }

    public void OnClickPage(int iPage)
    {
        if(iPage == 1)
        {
            FirstPanel.SetActive(true);
            SecondPanel.SetActive(false);
        }
        else
        {
            FirstPanel.SetActive(false);
            SecondPanel.SetActive(true);
        }
    }    
}
