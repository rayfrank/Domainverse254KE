using UnityEngine;
using TMPro;
using Fusion;

public class ShowRoomName : MonoBehaviour
{
    public TMP_Text roomText;

    void Start()
    {
        // Get the remembered value from FusionConnection
        string nameToShow = AvocadoShark.FusionConnection.LastSessionName;

        // Fallback if it wasn’t set (like when joining existing room)
        if (string.IsNullOrEmpty(nameToShow))
        {
            var runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && runner.SessionInfo.IsValid)
                nameToShow = runner.SessionInfo.Name;
        }

        string roomNumber = "?";

        // If the name starts with "Room-" strip it
        if (!string.IsNullOrEmpty(nameToShow))
        {
            if (nameToShow.StartsWith("Room-"))
                roomNumber = nameToShow.Replace("Room-", "");
            else
                roomNumber = nameToShow; // fallback for other naming styles
        }

        // ✅ Show it as "Room No: 255"
        roomText.text = $"Room No: {roomNumber}";
    }
}
