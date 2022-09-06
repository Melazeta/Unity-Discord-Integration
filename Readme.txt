La prima parte del readme si trova in un google doc che avevo inserito nella cartella di formazione melazeta che aveva condiviso Mitch (mi sembra) con tutti quanti. Di seguito il link ma non so se funzioni:
https://docs.google.com/document/d/1ayHb1NhliHeKFeBZCKKXkWjj9aRUk2XfoSokBI0aE08/edit#heading=h.q76b79z83kam

Di seguito la seconda parte.


* Spiegazione di quello che c'è nel progetto

Tutto il codice che riguarda discord è contenuto nella classe DiscordTest.

Affinchè funzioni tutto, è necessario che in DiscordTest sia settato un application id vero, bisogna creare un'applicazione nella dashboard di discord. Più info nel google doc, ma la guida è qui: 
https://discord.com/developers/docs/game-sdk/sdk-starter-guide#get-set-up

Ho lasciato dei commenti // TODO per le cose che devono essere sostituite.

Serve anche aver installato discord ed essere loggati con un utente.

Nella scena DiscordTest c'è una ui di pulsanti per usare le funzionalità della classe.

La parte in alto, cioè il bottone "set activity", l'input "this is a test!" e il bottone "clear activity" riguardano l'impostazione dell'attività in corso visibile su discord da sè stessi e dagli amici.

Subito sotto i bottoni "create lobby", "update lobby" e "delete lobby" riguardano la gestione di una lobby di giocatori. Più info nel google doc.

I bottoni sotto "voice" riguardano il canale voice della lobby, anche qui più info nel doc. Il bottone "open voice settings" fa crashare discord, non ho mai capito il motivo.

Il bottone "open invite overlay" serve per aprire su discord un popup per invitare qualcuno a giocare.

Il bottone "send bot message to server" invia il messaggio nell'input di fianco a destra in un server discord. Affinché funzioni è necessario impostare un webhook, vedi sotto.

Il bottone "get server messages" ottiene l'elenco di messaggi nel canale del server discord. Purtroppo il testo scritto nei messaggi è assente, non ho capito come ottenere il permesso di averlo. Per far funzionare questo il processo è più complicato, vedi sotto.

Il bottone "test storage" scrive e legge lo storage cloud offerto da discord.

In alto a destra c'è l'immagine del giocatore loggato.

Subito sotto l'elenco di amici discord.


* Inviare messaggi su server discord

Doc: https://discord.com/developers/docs/resources/webhook#execute-webhook

Per inviare messaggi su un server discord tramite un utente bot:

1) Andare in Server settings > Integrations
2) Aggiungere un Webhook
3) Selezionare il channel desiderato
4) Copiare il webhook url cliccando l’apposito bottone

N.B. il campo Name è il nome di default del bot, ma può essere sovrascritto da programma

A questo punto basta inviare una richiesta post all’url di cui al punto 4), non serve alcun token o autenticazione.

I parametri che possono essere passati nella richiesta sono indicati qui: https://discord.com/developers/docs/resources/webhook#execute-webhook-jsonform-params

Esempio (con postman delle mz unity utils):

postman.Post<object>("https://discordapp.com/api/webhooks/1016614603561635861/KmTa1w0yYnCAMdud_oHRicQOsIFSU3qc9-dl0q3Cd3ulw5Oljr7IvUpvDGmrOC1_NrNi",
    json: new
    {
        username = "Bot Name",  // sovrascrivo nome del bot
        content = "test from C#",
    }
);



* Leggere messaggi su server discord

N.B. per avere il contenuto dei messaggi serve un tipo di intent che non ho completamente capito, info qui:
https://discord.com/developers/docs/topics/gateway#message-content-intent
N.B. se segui questa guida e basta avrai l'elenco di messaggi ma non il testo di quei messaggi

Si possono leggere i messaggi di uno specifico canale conoscendo l’id.

Per avere l’id è necessario abilitare in discord il developer mode andando in User Settings > Advanced.
A questo punto entrare nel server, cliccare con il destro sul canale desiderato e poi su “Copy Id”.

È anche necessario creare un bot perché per ottenere i messaggi è necessario fornire un token di autorizzazione. Per creare un bot andare all’url:
https://discord.com/developers/applications/<APP_ID>/bot

Cliccare reset token e copiarlo nel codice.

In quella pagina selezionare in basso il permesso “Read Message History”. In fondo compare un numero come “Permission Integer” da usare per il passaggio successivo.

Ora è necessario aggiungere il bot al server. Per farlo basta navigare a un url costruito così:
https://discord.com/api/oauth2/authorize?client_id=<APP_ID>&&permissions=<PERMISSION_INTEGER>&scope=bot

(più dettagli qui: https://discordjs.guide/preparations/adding-your-bot-to-servers.html#adding-your-bot-to-servers)

Ora per ottenere la lista di messaggi vedi l'esempio di seguito, più info qui:
https://discord.com/developers/docs/resources/channel#get-channel-messages

postman.Get<object>(
    $"https://discordapp.com/api/channels/{channelId}/messages",
    new Dictionary<string, string>()
    {
        { "Authorization", "Bot <TOKEN_COPIATO_PRIMA>" }  // le parentesi angolari < > non devono rimanere
    },
    callback: response =>
    {
        if (!response.IsSuccessful())
            Debug.LogError("get messages failed: " + response.ToString());
        else
            Debug.Log("got messages: " + response.ToString());
    }
);

N.B. al posto di <object> dovrai fare una classe che contiene i campi da parsare, i messaggi contengono i campi indicati qui:
https://discord.com/developers/docs/resources/channel#message-object



