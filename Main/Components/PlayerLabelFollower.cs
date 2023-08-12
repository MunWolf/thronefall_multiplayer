using System;
using System.Net.Mime;
using Steamworks;
using TMPro;
using UnityEngine;

namespace ThronefallMP.Components;

public class PlayerLabelFollower : MonoBehaviour
{
    public Vector3 Offset { get; set; }
    public int TargetPlayer { get; set; }
    public Camera Camera { get; set; }

    private TextMeshPro _text;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
        _text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        var player = Plugin.Instance.PlayerManager.Get(TargetPlayer);
        if (player == null || player.Object == null)
        {
            return;
        }

        _text.transform.position = player.Object.transform.position + Offset;
        _text.transform.rotation = Quaternion.LookRotation(_text.transform.position - _camera.transform.position);
    }

    private void RefreshText()
    {
        var player = Plugin.Instance.PlayerManager.Get(TargetPlayer);
        _text.text = SteamFriends.GetFriendPersonaName(player.SteamID);
    }
}