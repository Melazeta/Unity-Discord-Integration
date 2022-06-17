using Discord;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiscordTest : MonoBehaviour
{
    public Text errorUi;
    public InputField detailsInput;

    [SerializeReference]
    Discord.Discord discord;

    // Start is called before the first frame update
    void Start()
    {
        var id = 973975457601028126;
        discord = new Discord.Discord(id, (UInt64)Discord.CreateFlags.Default);
        Debug.Log("init: " + discord);

        errorUi.text = string.Empty;

        LogDiscordUser();

        discord.GetUserManager().OnCurrentUserUpdate += LogDiscordUser;
    }

    private void Update()
    {
        discord.RunCallbacks();
    }

    private void OnDestroy()
    {
        discord.Dispose();
    }

    private void LogDiscordUser()
    {
        try
        {
            var user = discord.GetUserManager().GetCurrentUser();
            Debug.Log($"{user.Id}, {user.Username} {user.Avatar}");
        }
        catch (ResultException)
        {
            Debug.Log("discord user not ready");
        }
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
                    errorUi.text = "error updating activity: " + res;
            }
        );
    }

    public void ClearActivity()
    {
        discord.GetActivityManager().ClearActivity(res =>
        {
            if (res != Discord.Result.Ok)
                errorUi.text = "error clearing activity: " + res;
        });
    }
}
