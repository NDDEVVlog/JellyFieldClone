using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class BlockCountManager : MonoBehaviour
{
    public GameObject Container;
    public GameObject blockCountPrefab;

    private List<BlockCountUI> spawnedPrefab = new List<BlockCountUI>();

    public UnityEvent<int,int> onCollectionEvent = new UnityEvent<int,int>();

    public void SpawnBlock(CubeDataConfig cubeDataConfig, LevelConfigData levelConfigData)
    {
        if (cubeDataConfig == null || levelConfigData == null)
        {
            Debug.LogError("SpawnBlock called with null config");
            return;
        }

        if (Container == null || blockCountPrefab == null)
        {
            Debug.LogError("Container or blockCountPrefab is not assigned in BlockCountManager");
            return;
        }

        if (spawnedPrefab != null && spawnedPrefab.Count > 0)
        {
            foreach (BlockCountUI gO in spawnedPrefab)
            {
                if (gO != null)
                    Destroy(gO.gameObject);
            }
        }

        spawnedPrefab.Clear();

        onCollectionEvent ??= new UnityEvent<int, int>();
        onCollectionEvent.RemoveAllListeners();

        foreach (var goals in levelConfigData.missionGoals)
        {
            

            var blockCountgO = Instantiate(blockCountPrefab, Container.transform);
            BlockCountUI blockCountUI = blockCountgO.GetComponent<BlockCountUI>();
            if (blockCountUI == null)
            {
                Debug.LogError("BlockCountPrefab is missing BlockCountUI component.");
                Destroy(blockCountgO);
                continue;
            }

            blockCountUI.SetUp(cubeDataConfig.GetColor(goals.cubeId), goals.cubeId, goals.targetAmount);
            spawnedPrefab.Add(blockCountUI);
            onCollectionEvent.AddListener(blockCountUI.UpdateUI);
        }
    }

    public void InvokeEvent(int id,int amount)
    {   
        Debug.Log("collect id :" +id+ "with amount:"+amount);
        onCollectionEvent?.Invoke(id ,amount);
    }
}
