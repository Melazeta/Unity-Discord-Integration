using Discord;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiscordTest : MonoBehaviour
{
    public Text feedbackUi;
    public Text errorUi;
    public InputField detailsInput;
    public RawImage avatarImage;

    public Transform relationshipsParent;
    public GameObject relationshipPrefab;

    [SerializeReference]
    Discord.Discord discord;

    // Start is called before the first frame update
    void Start()
    {
        var id = 973975457601028126;
        discord = new Discord.Discord(id, (UInt64)Discord.CreateFlags.Default);

        feedbackUi.text = string.Empty;
        errorUi.text = string.Empty;

        LogDiscordUser();

        discord.GetUserManager().OnCurrentUserUpdate += LogDiscordUser;
        discord.GetUserManager().OnCurrentUserUpdate += FetchCurrentUserImage;

        discord.GetRelationshipManager().OnRefresh += OnRelationshipRefresh;
    }

    private void Update()
    {
        discord.RunCallbacks();
    }

    private void OnDestroy()
    {
        discord.Dispose();
    }

    private void Log(string message)
    {
        Debug.Log(message);
        feedbackUi.text = message;
        errorUi.text = string.Empty;
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
        errorUi.text = message;
        feedbackUi.text = string.Empty;
    }

    private void LogDiscordUser()
    {
        try
        {
            var user = discord.GetUserManager().GetCurrentUser();
            Log($"{user.Id}, {user.Username} {user.Avatar}");
        }
        catch (ResultException)
        {
            Log("discord user not ready");
        }
    }

    private void FetchCurrentUserImage()
    {
        var user = discord.GetUserManager().GetCurrentUser();
        FetchUserImage(user.Id, (res, texture) => avatarImage.texture = texture);
    }

    private void FetchUserImage(long userId, UnityAction<Result, Texture2D> callback)
    {
        discord.GetImageManager().Fetch(
            new ImageHandle()
            {
                Id = userId,
                Size = 512,
            },
            refresh: false,
            (res, handle) =>
            {
                Texture2D texture = null;

                if (res == Result.Ok)
                     texture = discord.GetImageManager().GetTexture(handle);
                else
                    LogError($"error fetching image of user {userId}: {res}");

                callback?.Invoke(res, texture);
            }
        );
    }

    public void UpdateActivity()
    {
        discord.GetActivityManager().UpdateActivity(
            new Discord.Activity()
            {
                Name = "The Name",  // non so cosa sia
                Details = detailsInput.text,  // seconda riga
                State = "could this be a state?",  // terza riga

                Party = new ActivityParty()  // N.B. necessario per invitare amici a joinare
                {
                    Id = "42",
                    Size = new PartySize()
                    {
                        CurrentSize = 1,
                        MaxSize = 4,
                    },
                },
                Secrets = new ActivitySecrets()  // N.B. necessario per invitare amici a joinare
                {
                    Join = "42424242424242424242",
                    //Match = "42",
                    //Spectate = "42",
                }
            },
            res =>
            {
                if (res != Discord.Result.Ok)
                    LogError("error updating activity: " + res);
            }
        );
    }

    public void ClearActivity()
    {
        discord.GetActivityManager().ClearActivity(res =>
        {
            if (res != Discord.Result.Ok)
                LogError("error clearing activity: " + res);
        });
    }

    public Lobby? lobby;

    public void CreateLobby()
    {
        LobbyTransaction transaction = discord.GetLobbyManager().GetLobbyCreateTransaction();

        transaction.SetCapacity(6);
        transaction.SetType(LobbyType.Public);
        transaction.SetMetadata("hello", "world");

        discord.GetLobbyManager().CreateLobby(transaction, (Result result, ref Lobby lobby) =>
        {
            if (result != Result.Ok)
            {
                LogError("create lobby failed: " + result);
                return;
            }

            this.lobby = lobby;

            Log("create lobby success, id: " + lobby.Id);
        });
    }

    public void UpdateLobby()
    {
        if (lobby == null)
        {
            Log("no lobby to update");
            return;
        }

        LobbyTransaction transaction = discord.GetLobbyManager().GetLobbyUpdateTransaction(lobby.Value.Id);
        transaction.SetCapacity(5);

        discord.GetLobbyManager().UpdateLobby(lobby.Value.Id, transaction, result =>
        {
            if (result != Result.Ok)
                LogError("update lobby failed: " + result);
            else
                Log("update lobby success");
        });
    }

    public void DeleteLobby()
    {
        if (lobby == null)
        {
            Log("no lobby to delete");
            return;
        }

        discord.GetLobbyManager().DeleteLobby(lobby.Value.Id, result =>
        {
            if (result != Result.Ok)
            {
                LogError("delete lobby failed: " + result);
                return;
            }

            Log("delete lobby success");
            lobby = null;
        });
    }

    public void ConnectVoiceToLobby()
    {
        if (lobby == null)
        {
            Log("no lobby to connect voice");
            return;
        }

        discord.GetLobbyManager().ConnectVoice(lobby.Value.Id, res =>
        {
            if (res == Result.Ok)
                Log("voice connected");
            else
                LogError("voice connection failed: " + res);
        });
    }

    public void DisconnectVoiceFromLobby()
    {
        if (lobby == null)
        {
            Log("no lobby to disconnect voice");
            return;
        }

        discord.GetLobbyManager().DisconnectVoice(lobby.Value.Id, res =>
        {
            if (res == Result.Ok)
                Log("voice disconnected");
            else
                LogError("voice disconnection failed: " + res);
        });
    }

    public void OpenVoiceOverlay()
    {
        if (!discord.GetOverlayManager().IsEnabled())
        {
            LogError("overlay is not enabled");
            return;
        }

        discord.GetOverlayManager().OpenVoiceSettings(res =>
        {
            if (res != Result.Ok)
                LogError("open voice settings failed: " + res);
        });
    }

    public void OpenActivityInvite()
    {
        //if (!discord.GetOverlayManager().IsEnabled())
        //{
        //    LogError("overlay is not enabled");
        //    return;
        //}

        discord.GetOverlayManager().OpenActivityInvite(ActivityActionType.Join, res =>
        {
            if (res != Result.Ok)
                LogError("open activity invite failed: " + res);
            else
                Log("open activity invite success");
        });
    }

    private void OnRelationshipRefresh()
    {
        foreach (Transform child in relationshipsParent)
            Destroy(child.gameObject);

        var manager = discord.GetRelationshipManager();

        manager.Filter((ref Relationship relationship) => true);

        try
        {
            for (uint i = 0; i < manager.Count(); i++)
            {
                var instance = Instantiate(relationshipPrefab, relationshipsParent);
                var user = manager.GetAt(i).User;

                instance.GetComponentInChildren<Text>().text = user.Username;
                FetchUserImage(user.Id, (res, texture) =>
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    instance.GetComponentInChildren<Image>().sprite = sprite;
                });
            }
        }
        catch (ResultException ex)
        {
            LogError("error showing relationships: " + ex.Message);
            throw;
        }
    }
}
