using System.Diagnostics.CodeAnalysis;
using AveManiaBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

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

    private static async Task HandleUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var chatType = message.Chat.Type;
        string senderName = message.From?.FirstName ?? message.From?.Username ?? "Someone";

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

    private static async Task HandleGroupMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, string senderName, string messageText)
    {
        if (Helpers.IsAveMania(messageText))
        {
            DbRepo repo = new DbRepo();
            int? entryId = repo.Check(messageText);
            if (entryId > 0)
            {
                await SendMultaMessage(botClient, cancellationToken, chatId, senderName, messageText, repo, entryId);
            }
            else
            {
                long unixTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                repo.Add(new AveMania(messageText, senderName, unixTimestamp, DateTime.Now));
            }
        }
    }

    private static async Task SendMultaMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, string senderName, string messageText, DbRepo repo, [DisallowNull] int? entryId)
    {
        AveMania? am = repo.Find(entryId.Value);
        await botClient.SendMessage(
            chatId: chatId,
            text: $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {am?.Author} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="chatId"></param>
    /// <param name="messageText"></param>
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

            switch (messageText.ToLower())
            {
                case "/init":
                {
                    new DbRepo().InitDataBase();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Db inizializzato con successo!",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/dp":
                    new DbRepo().DeleteDupicates();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Duplicati eliminati con successo!",
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
                case "/db":
                {
                    await SendDatabaseFile(botClient, cancellationToken, chatId); 
                    break;
                }
                default:
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Comandi disponibili:\n" +
                              "/s AVEMANIA - Cerca le avemanie - utile per evitare le multe\n" +
                              "/c - Conta le avemanie\n" +
                              "/h - Mostra questo messaggio\n" +
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