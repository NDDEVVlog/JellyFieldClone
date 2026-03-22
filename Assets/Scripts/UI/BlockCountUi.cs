using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockCountUI : MonoBehaviour
{   
    int _id;
    [SerializeField]
    private int remainingAmount;
    public Image blockUI;
    public TextMeshProUGUI tmp;

    public void SetUp(Color color,int id,int amount)
    {   
        
        this._id = id;
        blockUI.color = color;
        remainingAmount = amount;
        tmp.text=amount.ToString(); 
    }

    public void UpdateUI(int id, int collectedAmount)
    {
        if (id != this._id) return;

        remainingAmount -=  collectedAmount;
        
        tmp.text = remainingAmount >= 0 ? remainingAmount.ToString() : "0";

        
        if (remainingAmount <= 0)
        {
            tmp.color = Color.green; 
            blockUI.color = Color.grey;
        }
    }
}
