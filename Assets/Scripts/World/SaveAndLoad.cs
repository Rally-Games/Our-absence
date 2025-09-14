using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;

[System.Serializable]
public static class SaveAndLoad
{
    public static string savePathPlayerState { get; private set; } = Application.persistentDataPath + "/player_state.babczyk";
    public static string savePathSceneState { get; private set; } = Application.persistentDataPath + "/scene_state.babczyk";

    internal static void SavePlayerState(SavePlayerData.PlayerSaveData.Data data)
    {
        try
        {
            // Convert to JSON string
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            // Write to file
            File.WriteAllText(savePathPlayerState, json);

            Debug.Log($"Save created at {savePathPlayerState}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save: {ex}");
        }
    }


    internal static SavePlayerData.PlayerSaveData.Data LoadPlayerState()
    {
        if (!File.Exists(savePathPlayerState))
        {
            Debug.LogWarning("Save file not found!");
            return null;
        }

        try
        {
            // Read the JSON file
            string json = File.ReadAllText(savePathPlayerState);

            // Deserialize with Newtonsoft.Json (supports Dictionary)
            var data = JsonConvert.DeserializeObject<SavePlayerData.PlayerSaveData.Data>(json);

            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load save: {ex}");
            return null;
        }
    }

    internal static void SaveScenePickableItems(Dictionary<int, SceneManager.PickableItem> pickableItems)
    {
        try
        {
            // Convert to JSON string
            string json = JsonConvert.SerializeObject(pickableItems, Formatting.Indented);
            // Write to file
            File.WriteAllText(savePathSceneState, json);

            Debug.Log($"Save created at {savePathSceneState}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save: {ex}");
        }
    }

    internal static void LoadScenePickableItems(ref Dictionary<int, SceneManager.PickableItem> pickableItems)
    {
        if (!File.Exists(savePathSceneState))
        {
            Debug.LogWarning("Save file not found!");
            pickableItems = new Dictionary<int, SceneManager.PickableItem>();
        }

        try
        {
            // Read the JSON file
            string json = File.ReadAllText(savePathSceneState);

            // Deserialize with Newtonsoft.Json (supports Dictionary)
            var data = JsonConvert.DeserializeObject<Dictionary<int, SceneManager.PickableItem>>(json);
            Debug.Log(data[0].itemID);
            pickableItems = data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load save: {ex}");
            pickableItems = new Dictionary<int, SceneManager.PickableItem>();
        }
    }

}
