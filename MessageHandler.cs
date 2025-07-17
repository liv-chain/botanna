using System.Diagnostics.CodeAnalysis;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using static AveManiaBot.MessageHelper;
using File = System.IO.File;

namespace AveManiaBot;

public class MessageHandler(ITelegramBotClient botClient)
{
    /// <summary>
    /// Processes messages sent in a group or supergroup chat. Handles specific message content,
    /// determines whether a penalty or remark is to be issued, and manages user warnings or bans if limits are exceeded.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <param name="chatId">The unique identifier of the group or supergroup chat where the message was sent.</param>
    /// <param name="userId">The unique identifier of the user who sent the message.</param>
    /// <param name="senderName">The name of the user who sent the message. Includes both first name and last name if available.</param>
    /// <param name="messageText">The message text content received from the group or supergroup.</param>
    /// <param name="messageId"></param>
    /// <param name="messageDateTime"></param>
    /// <returns>A task representing the asynchronous operation of processing the group message.</returns>
    public async Task HandleGroupMessage(CancellationToken cancellationToken,
        long chatId, long? userId, string senderName, string messageText, int messageId, DateTime messageDateTime)
    {
        Console.WriteLine($"Received message from {chatId} - {senderName}: {messageText}");
        if (!Helpers.IsAveMania(messageText))
        {
            return;
        }

        DbRepo repo = new DbRepo();
        int? entryId = repo.CheckPenalty(messageText);
        if (entryId > 0)
        {
            var text = await SendPenaltyMessage(cancellationToken, chatId, senderName, messageText, repo, entryId);
            repo.Insert(new Penalty(text, senderName, 0, DateTime.Now));
        }
        else
        {
            long unixTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
            DbRepo.Insert(new AveMania(messageText, senderName, unixTimestamp, DateTime.Now, messageId));
        }

        (bool hasExceeded, int count, DateTime? dt, double timeSpan) activityCheck = CheckActivityArrest(senderName, repo, messageDateTime);
        int days = 0;
        DateTime banDate = DateTime.Now;
        Console.WriteLine($"Activity exceeded: {activityCheck.hasExceeded} - activity count {activityCheck.count}");
        bool activityArrest = false;
        bool penaltyArrest = false;
        switch (activityCheck.hasExceeded)
        {
            case true when activityCheck.count > AmConstants.ActivityWarningLimit + 1:
            {
                days += Math.Min(AmConstants.ActivityTimeSpanHours - (int)Math.Ceiling(activityCheck.timeSpan), 10);
                banDate = DateTime.Now.AddDays(days);
                activityArrest = true;
                await botClient.SendMessage(
                    chatId: chatId,
                    text:
                    $"{AmConstants.MalePoliceEmoji} ARRESTO: {senderName} sarà in prigione per {days} giorni fino al {banDate:g} {AmConstants.FemalePoliceEmoji}",
                    cancellationToken: cancellationToken);

                break;
            }
            case true:
                await RemarkUser(botClient, cancellationToken, chatId, senderName, activityCheck.dt);
                break;
        }

        var checkPenalResult = await CheckPenaltyArrest(senderName, repo, messageDateTime);

        if (checkPenalResult.hasExceeded)
        {
            days += 7;
            banDate = DateTime.Now.AddDays(days);
            penaltyArrest = true;
            await botClient.SendMessage(
                chatId: chatId,
                text:
                $"{AmConstants.PoliceCarEmoji} ARRESTO per eccesso di multe: {senderName} sarà in prigione per {days} giorni fino al {banDate:g} {AmConstants.MalePoliceEmoji}",
                cancellationToken: cancellationToken);
        }

        if (activityArrest || penaltyArrest)
        {
            await BanChatMember(botClient, chatId, userId, cancellationToken, banDate, days);
        }

        if (activityArrest && penaltyArrest)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text:
                $"{AmConstants.MalePoliceEmoji}{AmConstants.FemalePoliceEmoji} MEGA ARRESTO COMBINATO!!! {AmConstants.PoliceCarEmoji}{AmConstants.PoliceCarEmoji}",
                cancellationToken: cancellationToken);
        }

    }

    /// <summary>
    /// Handles private messages sent to the bot, parses the message text, and performs operations based on specific commands.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <param name="chatId">The unique identifier of the chat where the message originates.</param>
    /// <param name="messageText">The text of the private message received by the bot.</param>
    /// <param name="messageId"></param>
    /// <returns>A task that represents the asynchronous operation of processing the private message.</returns>
    public async Task HandlePrivateMessage(CancellationToken cancellationToken,
        long chatId, string messageText, int? messageId = null)
    {
        try
        {
            
            Console.WriteLine(messageText);
            
            // commands with arguments
            if (messageText.ToLower().StartsWith("/s ") || messageText.ToLower().StartsWith("s "))
            {
                await SearchResults(cancellationToken, chatId, messageText);
                return;
            }

            if (messageText.ToLower().StartsWith("/sqlcmd "))
            {
                new DbRepo().Execute(messageText);
                return;
            }

            if (messageText.ToLower().StartsWith("ech "))
            {
                await Echo(botClient, AmConstants.AmChatId,
                    messageText.Replace("ech ", "", StringComparison.OrdinalIgnoreCase), cancellationToken);
                return;
            }

            if (messageText.ToLower().StartsWith("/add"))
            {
                messageText = messageText.Replace("/add", "");
                DbRepo.Insert(new AveMania(messageText.TrimStart(), "Someone", 0, DateTime.Now, messageId));
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
                    new DbRepo().EnsureSchemaAndUpdate();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Db inizializzato con successo!",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/telepr":
                {
                    // await new DbRepo().ProcessTelegramMessages(botClient, cancellationToken);

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Metodo commentato diolana",
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
                case "c":
                {
                    var count = new DbRepo().Count();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Sono state scritte {count} avemanie \ud83d\ude0a",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "ca":
                {
                    await ShowCountStats(cancellationToken, chatId);
                    break;
                }
                case "act":
                {
                    await ShowActivity(cancellationToken, chatId);
                    break;
                }
                case "killme":
                {
                    await botClient.Close(cancellationToken: cancellationToken);
                    break;
                }
                case "p":
                {
                    await ShowPenalties(cancellationToken, chatId);
                    break;
                }
                case "r":
                {
                    var r = new DbRepo().GetRandom(3);
                    string text = string.Join("\n", r.Select(x => x.ToString()));
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Ecco 3 avemanie per te. Spero ti vadano di traverso. \n" + text,
                        cancellationToken: cancellationToken);
                    break;
                }
                case "rrr":
                {
                    var r = new DbRepo().GetRandom(100);
                    for (int i = 0; i < 100; i += 10)
                    {
                        string text = string.Join("\n", r.Skip(i).Take(10)
                            .Select(x => x.ToString()));

                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "\n" + text,
                            cancellationToken: cancellationToken);
                    }

                    break;
                }
                case "db":
                {
                    await SendDatabaseFile(cancellationToken, chatId);
                    break;
                }
                case "v":
                {
                    string assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString() ??
                                             "Version not available";
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"L'attuale versione è: {assemblyVersion}",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "d":
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
                              "s AVEMANIA - Cerca le avemanie - utile per evitare le multe\n" +
                              "c - Conta le avemanie\n" +
                              "p - Mostra i dati sulle multe\n" +
                              "d - Mostra le avemanie delle ultime 24 ore\n" +
                              "r - Restituisce 3 avemanie a caso\n" +
                              "rrr - Restituisce 100 avemanie a caso\n" +
                              "v - Numero di versione\n" +
                              "db - Scarica il db",
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

    private async Task ShowCountStats(CancellationToken cancellationToken, long chatId)
    {
        Dictionary<string, int> countStatsForAuthor = await new DbRepo().GetAveManiaCountPerAuthor();
        var ordered = countStatsForAuthor
            .OrderByDescending(kv => kv.Value)
            .Select((kv, index) =>
            {
                string medal = index switch
                {
                    0 => "🥇 ",
                    1 => "🥈 ",
                    2 => "🥉 ",
                    _ => ""
                };
                string author = string.IsNullOrEmpty(kv.Key) ? "Unknown" : kv.Key;
                return $"{medal} {author,-20} → {kv.Value}";
            });

        var sb = new StringBuilder();
        foreach (var line in ordered)
        {
            sb.AppendLine(line);
        }

        await botClient.SendMessage(
            chatId: chatId,
            sb.ToString(),
            cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Triggers the bot to send a message to a specific chat.
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="chatId">The unique identifier for the chat.</param>
    /// <param name="message">The message to send to the chat.</param>
    /// <param name="cancellationToken"></param>
    private static async Task Echo(ITelegramBotClient botClient, long chatId, string message,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: message,
            cancellationToken: cancellationToken);
    }

    private async Task ShowPenalties(CancellationToken cancellationToken, long chatId)
    {
        var penaltiesForAuthor = new DbRepo().GetPenaltiesForAllAuthors();
        var sb = new StringBuilder();

        foreach (var author in penaltiesForAuthor)
        {
            string authorName = string.IsNullOrEmpty(author.Key) ? "Sconosciuto" : author.Key;
            sb.AppendLine($"{authorName} ha preso {author.Value} multe");
        }

        sb.AppendLine();
        sb.AppendLine("Percentuale di multe");
        sb.AppendLine();

        var ratios = new DbRepo().GetPenaltiesRatioStats();
        var positiveRatios = ratios.Where(r => r.Value > 0).ToList();
        var count = positiveRatios.Count;

        var ordered = positiveRatios
            .OrderBy(kv => kv.Value)
            .Select((kv, index) =>
            {
                string medal = index switch
                {
                    0 => "🥇 ",
                    1 => "🥈 ",
                    2 => "🥉 ",
                    _ when index >= count - 3 => "💩 ",
                    _ => ""
                };
                string author = string.IsNullOrEmpty(kv.Key) ? "Unknown" : kv.Key;
                string percentage = kv.Value.ToString("P");
                return $"{medal} {author,-20} → {percentage}";
            });

        foreach (var line in ordered)
        {
            sb.AppendLine(line);
        }

        await botClient.SendMessage(
            chatId: chatId,
            sb.ToString(),
            cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Sends a notification message to the chat when a duplicate message is detected, indicating the original author and timestamp.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <param name="chatId">The unique identifier of the chat where the message needs to be sent.</param>
    /// <param name="senderName">The name of the user who sent the duplicate message.</param>
    /// <param name="messageText">The text of the duplicate message.</param>
    /// <param name="repo">The database repository used to retrieve information about stored messages.</param>
    /// <param name="originalAveManiaId">The identifier of the original message entry in the database.</param>
    /// <returns>A task representing the asynchronous operation of sending the notification message.</returns>
    private async Task<string> SendPenaltyMessage(CancellationToken cancellationToken, long chatId, string senderName, string messageText, DbRepo repo,
        [DisallowNull] int? originalAveManiaId)
    {
        AveMania? am = repo.Find(originalAveManiaId.Value);

        var auth = am?.Author;
        if (string.IsNullOrWhiteSpace(auth))
        {
            auth = "Utente Facebook";
        }

        string text =
            $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {auth} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f";

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return text;
    }

    private async Task ShowActivity(CancellationToken cancellationToken,
        long chatId)
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

    private async Task SendDatabaseFile(CancellationToken cancellationToken,
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

    private async Task SearchResults(CancellationToken cancellationToken,
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
}