using Steamworks;
using TMPro;
using UnityEngine;

namespace ThronefallMP.Components;

public class PlayerLabelFollower : MonoBehaviour
{
    public int TargetPlayer { get; set; }
    
    private TextMeshPro _text;
    private Camera _camera; 
    private Vector3 _offset;

    private void Start()
    {
        _camera = Camera.main;
        _text = GetComponent<TextMeshPro>();
        _text.fontSize = 12;
        _text.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.outlineColor = Color.black;
        _text.outlineWidth = 0.2f;
        _offset = new Vector3(0.15f, 6, -0.15f);
        _text.transform.rotation = Quaternion.Euler(54.17f, 45.09f, 0);
        RefreshText();
    }

    private void Update()
    {
        var player = Plugin.Instance.PlayerManager.Get(TargetPlayer);
        if (player == null || player.Object == null)
        {
            Destroy(gameObject);
            return;
        }

        _text.transform.position = Vector3.Lerp(
            _text.transform.position,
            player.Object.transform.position + _offset,
            0.7f
        );
    }

    private void RefreshText()
    {
        var player = Plugin.Instance.PlayerManager.Get(TargetPlayer);
        if (SteamManager.Initialized && player != null)
        {
            _text.text = SteamFriends.GetFriendPersonaName(player.SteamID);
            Plugin.Log.LogInfo($"Found name for {player.Id} = '{_text.text}'");
        }
    }
}