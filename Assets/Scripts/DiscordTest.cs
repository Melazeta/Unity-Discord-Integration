using Discord;
using MZ.Rest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiscordTest : MonoBehaviour
{
    public Postman postman;

    public Text feedbackUi;
    public Text errorUi;
    public InputField detailsInput;
    public RawImage avatarImage;

    public Transform relationshipsParent;
    public RelationshipEntry relationshipPrefab;

    public InputField sendMessageInput;

    [SerializeReference]
    Discord.Discord discord;

    // Start is called before the first frame update
    void Start()
    {
        //var id = 973975457601028126;
        var id = 1016643780968988733;  // TODO: inserire l'id della propria applicazione discord
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
                RelationshipEntry instance = Instantiate(relationshipPrefab, relationshipsParent);
                var relationship = manager.GetAt(i);

                instance.usernameText.text = relationship.User.Username;
                instance.activityText.text = relationship.Presence.Activity.Name;
                FetchUserImage(relationship.User.Id, (res, texture) =>
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    instance.userImage.sprite = sprite;
                });
            }
        }
        catch (ResultException ex)
        {
            LogError("error showing relationships: " + ex.Message);
            throw;
        }
    }

    [ContextMenu("Test storage")]
    public void TestStorage()
    {
        // copiato e adattato da:
        // https://discord.com/developers/docs/game-sdk/storage#example-saving-reading-deleting-and-checking-data

        var storageManager = discord.GetStorageManager();

        // Create some nonsense data
        var contents = new byte[20000];
        var random = new System.Random();
        random.NextBytes(contents);

        // Write the data asynchronously
        storageManager.WriteAsync("foo", contents, res =>
        {
            // Get our list of files and iterate over it
            for (int i = 0; i < storageManager.Count(); i++)
            {
                var file = storageManager.StatAt(i);
                Log($"file: {file.Filename} size: {file.Size} last_modified: {file.LastModified}");
            }

            // Let's read just a small chunk of data from the "foo" key
            storageManager.ReadAsyncPartial("foo", 400, 50, (result, data) =>
            {
                Log($"partial contents of foo match {Enumerable.SequenceEqual(data, new ArraySegment<byte>(contents, 400, 50))}");
            });

            // Now let's read all of "foo"
            storageManager.ReadAsync("foo", (result, data) =>
            {
                Log($"length of contents {contents.Length} data {data.Length}");
                Log($"contents of foo match {Enumerable.SequenceEqual(data, contents)}");

                // We just read it, but let's make sure "foo" exists
                Log($"foo exists? {storageManager.Exists("foo")}");

                // Now delete it
                storageManager.Delete("foo");

                // Make sure it was deleted
                Log($"post-delete foo exists? {storageManager.Exists("foo")}");
            });
        });
    }

    [ContextMenu("Send message to server")]
    public void SendMessageToServer()
    {
        string text = "test from C#";

        if (sendMessageInput != null && !string.IsNullOrEmpty(sendMessageInput.text))
            text = sendMessageInput.text;

        postman.Post<object>(
            // TODO: impostare un webhook valido 
            "https://discordapp.com/api/webhooks/1016614603561635861/KmTa1w0yYnCAMdud_oHRicQOsIFSU3qc9-dl0q3Cd3ulw5Oljr7IvUpvDGmrOC1_NrNi",
            json: new
            {
                username = "Bot Name",  // questo sovrascrive il nome del bot
                content = text,
            },
            callback: resposne =>
            {
                if (resposne.IsSuccessful())
                    Log("message sent succesfully");
                else
                    LogError("message send failed");
            }
        );
    }

    [ContextMenu("Get messages from server channel")]
    public void GetMessagesFromServerChannel()
    {
        // TODO: impostare l'id di un canale di un server
        long channelId = 1016614524066988106;

        postman.Get<object>(
            $"https://discordapp.com/api/channels/{channelId}/messages",
            new Dictionary<string, string>()
            {
                // TODO: inserire il token del bot preso dall'applicazione discord
                // N.B. occhio che se viene fatto il push del token su una repo pubblica discord effettua il reset del token
                { "Authorization", "Bot MTAxNjY0Mzc4MDk2ODk4ODczMw.G1HZgC.38XiNj92TSxjRdXEVh11nV2mDqADXCbvQW8kRk" }
            },
            callback: response =>
            {
                if (!response.IsSuccessful())
                    LogError("get messages failed: " + response.ToString());
                else
                    Log("got messages: " + response.ToString());
            }
        );
    }
}
