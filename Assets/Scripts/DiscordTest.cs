using Discord;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiscordTest : MonoBehaviour
{
    public Text feedbackUi;
    public Text errorUi;
    public InputField detailsInput;
    public RawImage avatarImage;

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
        discord.GetUserManager().OnCurrentUserUpdate += FetchImage;
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

    private void FetchImage()
    {
        var user = discord.GetUserManager().GetCurrentUser();
        discord.GetImageManager().Fetch(
            new ImageHandle()
            {
                Id = user.Id,
                Size = 512,
            },
            refresh: false,
            (res, handle) =>
            {
                if (res == Result.Ok)
                {
                    var texture = discord.GetImageManager().GetTexture(handle);
                    avatarImage.texture = texture;
                }
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
        discord.GetOverlayManager().OpenVoiceSettings(res =>
        {
            if (res != Result.Ok)
                LogError("open voice settings failed: " + res);
        });
    }
}
