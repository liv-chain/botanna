using System.Diagnostics.CodeAnalysis;
using AveManiaBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

// ReSharper disable once CheckNamespace
class Program
{
    static async Task Main(string[] args)
    {
        string botToken = "7997826290:AAGdFuQNlwjynaheYTV6wq7kBlYr5WBWNQw";
        var botClient = new TelegramBotClient(botToken);

        // Start receiving updates
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationToken: cts.Token);

        var me = await botClient.GetMe(cancellationToken: cts.Token);
        Console.WriteLine($"Bot {me.Username} is up and running!");

        Console.ReadLine();
        await cts.CancelAsync(); // Stop the bot when the program exits
    }

    /// <summary>
    /// Handles incoming updates from the Telegram API, processes messages based on their chat type, and delegates them to the appropriate handling methods.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="update">The incoming update object containing information about the event (e.g., messages, commands) received by the bot.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <returns>A task representing the asynchronous operation of processing the update.</returns>
    private static async Task HandleUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var chatType = message.Chat.Type;
        string senderName = message.From?.FirstName ?? message.From?.Username ?? "Someone";
        
        // Check if the update is about a new member being added to the group
        // if (update.Message?.NewChatMembers?.Any() ?? false)
        // {
        //     foreach (var newMember in update.Message.NewChatMembers)
        //     {
        //         string newMemberName = newMember.FirstName ?? newMember.Username ?? "A new member";
        //         await botClient.SendMessage(
        //             chatId: chatId,
        //             text: $"Welcome {newMemberName} to the group!",
        //             cancellationToken: cancellationToken);
        //     }
        //     return;
        // }
        
        // Check if the message is from a group
        if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
        {
            await HandleGroupMessage(botClient, cancellationToken, chatId, senderName, messageText);
        }
        else
        {
            await HandlePrivateMessage(botClient, cancellationToken, chatId, messageText);
        }
    }

    /// <summary>
    /// Handles incoming group messages, determines if they match specific criteria, and processes them accordingly by checking for duplicates or saving new messages.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <param name="chatId">The unique identifier of the group chat where the message was received.</param>
    /// <param name="senderName">The name of the user who sent the message in the group.</param>
    /// <param name="messageText">The text of the message received in the group.</param>
    /// <returns>A task representing the asynchronous operation of processing the group message.</returns>
    private static async Task HandleGroupMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, string senderName, string messageText)
    {
        if (Helpers.IsAveMania(messageText))
        {
            DbRepo repo = new DbRepo();
            int? entryId = repo.Check(messageText);
            if (entryId > 0)
            {
                var text = await SendPenaltyMessage(botClient, cancellationToken, chatId, senderName, messageText, repo,
                    entryId);
                
                repo.Add(new Penalty(text, senderName, 0, DateTime.Now));
            }
            else
            {
                long unixTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                repo.Add(new AveMania(messageText, senderName, unixTimestamp, DateTime.Now));
            }
        }
    }

    /// <summary>
    /// Handles private messages sent to the bot, parses the message text, and performs operations based on specific commands.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <param name="chatId">The unique identifier of the chat where the message originates.</param>
    /// <param name="messageText">The text of the private message received by the bot.</param>
    /// <returns>A task that represents the asynchronous operation of processing the private message.</returns>
    private static async Task HandlePrivateMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, string messageText)
    {
        try
        {
            if (messageText.ToLower().StartsWith("/s"))
            {
                await SearchResults(botClient, cancellationToken, chatId, messageText);
                return;
            }

            if (messageText.ToLower().StartsWith("/add"))
            {
                messageText = messageText.Replace("/add", "");
                new DbRepo().Add(new AveMania(messageText.TrimStart(), "Someone", 0, DateTime.Now));
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Avemania aggiunta!",
                    cancellationToken: cancellationToken);
                return;
            }

            switch (messageText.ToLower())
            {
                case "/init":
                {
                    new DbRepo().InitDataBase(false);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Db inizializzato con successo!",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/dp":
                    new DbRepo().DeleteDuplicates();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Duplicati eliminati con successo!",
                        cancellationToken: cancellationToken);
                    break;
                case "/c":
                {
                    var count = new DbRepo().Count();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Sono state scritte {count} avemanie \ud83d\ude0a",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/act":
                {
                    await ShowActivity(botClient, cancellationToken, chatId);
                    break;
                }
                case "/p":
                {
                    await ShowPenalties(botClient, cancellationToken, chatId);
                    break;
                }
                case "/r":
                {
                    var r = new DbRepo().GetRandom(3);
                    string text = string.Join("\n", r.Select(x => x.ToString()));
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Ecco 3 avemanie per te. Spero ti vadano di traverso. \n" + text,
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/db":
                {
                    await SendDatabaseFile(botClient, cancellationToken, chatId);
                    break;
                }
                case "/d":
                {
                    DbRepo repo = new DbRepo();
                    List<AveMania> results = repo.GetLast(24);
                    if (results.Count > 0)
                    {
                        string text = string.Join("\n", results.Select(x => x.ToString()));
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Guarda un po' qua cosa ho trovato\n" + text,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Nessuno ha scritto un cazzo",
                            cancellationToken: cancellationToken);
                    }

                    break;
                }

                default:
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Comandi disponibili:\n" +
                              "/s AVEMANIA - Cerca le avemanie - utile per evitare le multe\n" +
                              "/c - Conta le avemanie\n" +
                              "/p - Mostra i dati sulle multe\n" +
                              "/d - Mostra le avemanie delle ultime 24 ore\n" +
                              "/h - Mostra questo messaggio\n" +
                              "/r - Restituisce 3 avemanie a caso\n" +
                              "/act - Dati sull'attività dei partecipanti\n" +
                              "/db - Scarica il db",
                        cancellationToken: cancellationToken);
                    break;
            }
        }
        catch (Exception e)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: e.Message,
                cancellationToken: cancellationToken);
        }
    }

    private static async Task ShowPenalties(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
    {
        var penaltiesForAuthor = new DbRepo().GetPenaltiesForAllAuthors();

        string GetDesc(string current, KeyValuePair<string, int> author)
        {
            string authorName = string.IsNullOrEmpty(author.Key) ? "Sconosciuto" : author.Key;
            return current + $"{authorName} ha preso {author.Value} multe\n";
        }
        
        string text = penaltiesForAuthor.Aggregate(string.Empty, GetDesc);

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a notification message to the chat when a duplicate message is detected, indicating the original author and timestamp.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <param name="chatId">The unique identifier of the chat where the message needs to be sent.</param>
    /// <param name="senderName">The name of the user who sent the duplicate message.</param>
    /// <param name="messageText">The text of the duplicate message.</param>
    /// <param name="repo">The database repository used to retrieve information about stored messages.</param>
    /// <param name="entryId">The identifier of the original message entry in the database.</param>
    /// <returns>A task representing the asynchronous operation of sending the notification message.</returns>
    private static async Task<string> SendPenaltyMessage(ITelegramBotClient botClient,
        CancellationToken cancellationToken,
        long chatId, string senderName, string messageText, DbRepo repo, [DisallowNull] int? entryId)
    {
       
        AveMania? am = repo.Find(entryId.Value);

        string text =
            $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {am?.Author} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f";

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken).ConfigureAwait(false);


        return text;
    }

    private static async Task ShowActivity(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
    {
        var daysForAuthor = new DbRepo().GetDaysSinceLastMessageForAllAuthors();

        string GetDesc(string current, KeyValuePair<string, int> author)
        {
            string authorName = string.IsNullOrEmpty(author.Key) ? "Sconosciuto" : author.Key;
            return current + $"{authorName} ha scritto l'ultima avemania il {author.Value} giorni fa\n";
        }

        string text = daysForAuthor.Aggregate(string.Empty, GetDesc);

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken);
    }


    private static async Task SendDatabaseFile(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId)
    {
        try
        {
            string dbFilePath = "./ave_mania.db";
            if (File.Exists(dbFilePath))
            {
                using var stream = new FileStream(dbFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await botClient.SendDocument(
                    chatId: chatId,
                    document: new InputFileStream(stream, "database.db"),
                    caption: "Here is the SQLite database file.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Database file not found. Ensure the database path is correct!",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: $"An error occurred while sending the database: {ex.Message}",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SearchResults(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId,
        string messageText)
    {
        string am = Helpers.GetArgument(messageText);
        if (Helpers.IsAveMania(am.ToUpper()))
        {
            DbRepo repo = new DbRepo();
            var argument = Helpers.GetArgument(messageText);
            List<AveMania> results = repo.FindMessagesContaining(argument.ToUpper());
            if (results.Count > 0)
            {
                string text = string.Join("\n", results.Select(x => x.ToString()));
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Guarda un po' qua cosa ho trovato\n" + text,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Vai che non è multa",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text:
                "Che cazzo stai blaterando, non hai scritto un'avemania. Scrivi /s PAROLA per cercare tutte le avemanie che contengono PAROLA",
                cancellationToken: cancellationToken);
        }
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}