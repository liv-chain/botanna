using System.Diagnostics.CodeAnalysis;
using AveManiaBot;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using Message = Telegram.Bot.Types.Message;

// ReSharper disable once CheckNamespace
class Program
{
    private static TelegramBotClient _botClient;
    private static readonly string BotToken = "7997826290:AAGdFuQNlwjynaheYTV6wq7kBlYr5WBWNQw";
    public static readonly long AmChatId = -1002381222429;
    private static readonly string MalePoliceEmoji = "\U0001F46E\u200D\u2642\U0000FE0F"; // 👮‍♂️
    private static Timer? _timer;

    static readonly List<string> Remarks =
    [
        "messo il turbo oggi, eh?",
        "sei un podcast vivente ma hai rotto il cazzo.",
        "stai facendo il monologo finale di un film, o c’è una pausa da qualche parte?",
        "vuoi un microfono, o ti senti già abbastanza amplificato?",
        "sei già al capitolo 3 del tuo libro di puttanate?",
        "minchia oh, non ci sei solo tu qua eh.",
        "vai a scavare buche nel Tagliamento.",
        "forse è il momento di passare la parola agli altri.",
        "vai a giocare con la merda nella tundra.",
        "palettaaaaaaaaaaaaaaaaa!",
        "mi piacerebbe sentire anche il punto di vista di qualcun altro di voi stronzetti.",
        "grazie per la tua passione, ma non hai un cazzo da fare oggi?",
        "puoi riassumere? Abbiamo poco tempo per leggere tutte le cagate che scrivi.",
        "hai coperto ogni dettaglio delle tue minchiate, possiamo passare al prossimo argomento?",
        "facciamo un break dalle minchiate, che dici?",
        "va bene, ho capito. Possiamo chiudere il discorso qui che hai scardinato lo scroto?",
        "ma smettila di fare il gazzabbubbo di turno, che qui non siamo al circo!",
        "se continui a parlare così, finisci dritto dritto nel manuale del perfetto spruzzafuffa.",
        "ma sei proprio un mestolone di gorgoglione oggi, eh?",
        "oh, gazzabbubbo ufficiale, la parola la passiamo anche agli altri o no?",
        "sembri un frastugliacazzi, vai avanti all’infinito!",
        "basta con questa manfrina da scatafasco ambulante!",
        "ma quanto hai bevuto dal calderone della logorrina oggi?",
        "Hai intenzione di brevettare questa infinita marea di cagate?",
        "Va bene, ho capito, il mondo ruota intorno alla tua voce oggi!",
        "Non ti stanchi mai di sentire il suono delle tue stesse stronzate?",
        "Aspetta, fammi respirare tra una frase e l’altra minchia!",
        "Se parlassi un po’ di meno, potremmo forse risolvere questo problema prima di domani.",
        "Se le parole fossero denaro, potresti comprarti un biglietto per andare affanculo.",
        "Mi fai sentire come se stessi leggendo il manuale di un elettrodomestico.",
        "Hai già conquistato il trofeo del scassacazzi dell’anno, possiamo passare oltre?"
    ];
    
    
    static async Task Main(string[] args)
    {
        await RunBot();
        _timer = new Timer(async _ => await RestartBot(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    private static async Task RestartBot()
    {
        await _botClient.Close();
        await RunBot();
    }

    private static async Task RunBot()
    {
        _botClient = new TelegramBotClient(BotToken);

        // Start receiving updates
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };
        
        new DbRepo().Check("diocannone");
        
        _botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationToken: cts.Token);
        
        try
        {
            var me = await _botClient.GetMe(cancellationToken: cts.Token);
            Console.WriteLine($"Bot {me.Username} is up and running!");
        }
        catch (Exception)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine(
                            $"Retry {retryCount} for GetMe due to: {exception.Message}. Waiting {timeSpan.TotalSeconds} seconds before retrying...");
                    });
            try
            {
                var me = await retryPolicy.ExecuteAsync(() => _botClient.GetMe(cancellationToken: cts.Token));
                Console.WriteLine($"Bot {me.Username} is up and running!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve bot information: {ex.Message}");
            }
        }

        Console.ReadLine();
        await cts.CancelAsync(); // Stop the bot when the program exits
    }

    /// <summary>
    /// Handles incoming updates from the Telegram API, processes messages based on their chat type,
    /// and delegates them to the appropriate handling methods.
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

        await ProcessMessageBasedOnChatType(botClient, cancellationToken, message, messageText);
    }

    /// <summary>
    /// Processes a message received by the bot and determines the appropriate action
    /// based on the type of chat (group, supergroup, or private).
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Token used to propagate notification that the operation should be canceled.</param>
    /// <param name="message">The message object containing details of the received message.</param>
    /// <param name="messageText">The text content of the received message.</param>
    /// <returns>A task representing the asynchronous operation of handling the message based on its chat type.</returns>
    private static async Task ProcessMessageBasedOnChatType(ITelegramBotClient botClient,
        CancellationToken cancellationToken, Message message, string messageText)
    {
        var chatId = message.Chat.Id;
        var chatType = message.Chat.Type;
        var userId = message.From?.Id!;
        string senderName = message.From?.FirstName!;

        if (!string.IsNullOrEmpty(message.From?.LastName))
            senderName += $" {message.From?.LastName}";

        // Check if the message is from a group
        if (chatType == ChatType.Group || chatType == ChatType.Supergroup)
        {
            await HandleGroupMessage(botClient, cancellationToken, chatId, userId, senderName, messageText);
        }
        else
        {
            await HandlePrivateMessage(botClient, cancellationToken, chatId, messageText);
        }
    }


    /// <summary>
    /// Processes messages sent in a group or supergroup chat. Handles specific message content,
    /// determines whether a penalty or remark is to be issued, and manages user warnings or bans if limits are exceeded.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to send messages or perform chat actions.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be canceled.</param>
    /// <param name="chatId">The unique identifier of the group or supergroup chat where the message was sent.</param>
    /// <param name="userId">The unique identifier of the user who sent the message.</param>
    /// <param name="senderName">The name of the user who sent the message. Includes both first name and last name if available.</param>
    /// <param name="messageText">The message text content received from the group or supergroup.</param>
    /// <returns>A task representing the asynchronous operation of processing the group message.</returns>
    private static async Task HandleGroupMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, long? userId, string senderName, string messageText)
    {
        Console.WriteLine($"Received message from {chatId} - {senderName}: {messageText}");

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

            var limit = 3;
            (bool hasExceeded, int count, DateTime? dt) checkResult = repo.HasAuthorExceededLimit(senderName, limit);
            Console.WriteLine($"Exceeded: {checkResult.hasExceeded} - {checkResult.count}");
            
            switch (checkResult.hasExceeded)
            {
                case true when checkResult.count > limit + 1:
                {
                    DateTime? banDate = await BanChatMember(botClient, chatId, userId, cancellationToken);
                    if (banDate.HasValue)
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text:
                            $"{MalePoliceEmoji} ARRESTO: {senderName} sarà in prigione fino al {banDate.Value:g} {MalePoliceEmoji}",
                            cancellationToken: cancellationToken);
                    }
                    return;
                }
                case true:
                    await RemarkUser(botClient, cancellationToken, chatId, senderName, checkResult.dt);
                    break;
            }
        }
    }

    private static async Task RemarkUser(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId,
        string senderName, DateTime? dt)
    {
        string timeMsg = string.Empty;
        if (dt != null)
        {
            timeMsg = $" - Potrai riprendere a scrivere senza incorrere in arresto alle {dt.Value:t} ";
        }
        await botClient.SendMessage(
            chatId: chatId,
            text: $"{senderName}, {GetRandomRemark()} Al prossimo richiamo di oggi scatterà l'arresto.{timeMsg}{MalePoliceEmoji}",
            cancellationToken: cancellationToken);
    }

    private static string GetRandomRemark()
    {
        if (Remarks == null || Remarks.Count == 0)
        {
            throw new ArgumentException("La lista non può essere nulla o vuota.");
        }

        Random random = new Random();
        int index = random.Next(Remarks.Count);
        return Remarks[index];
    }

    /// <summary>
    /// Triggers the bot to send a message to a specific chat.
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="chatId">The unique identifier for the chat.</param>
    /// <param name="message">The message to send to the chat.</param>
    static async Task TriggerBotMessage(ITelegramBotClient botClient, long chatId, string message,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: message,
            cancellationToken: cancellationToken);
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
            if (messageText.ToLower().StartsWith("/s "))
            {
                await SearchResults(botClient, cancellationToken, chatId, messageText);
                return;
            }

            if (messageText.ToLower().StartsWith("/sqlcmd "))
            {
                ExecuteSQLCode(messageText);
                return;
            }

            if (messageText.ToLower().StartsWith("/ech "))
            {
                await TriggerBotMessage(botClient, AmChatId, messageText.Replace("/ech ", "", StringComparison.OrdinalIgnoreCase), cancellationToken);
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
                case "/telepr":
                {
                    await new DbRepo().ProcessTelegramMessages(botClient, cancellationToken);
                    
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Dati ripristinati con successo!",
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
                case "/clr":
                {
                    var count = new DbRepo().ClearPenalties();
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Sono state cancellate {count} avemanie \ud83d\ude0a",
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/act":
                {
                    await ShowActivity(botClient, cancellationToken, chatId);
                    break;
                }                
                case "/killme":
                {
                    await botClient.Close(cancellationToken: cancellationToken);
                    break;
                }
                case "/p":
                {
                    await ShowPenalties(botClient, cancellationToken, chatId);
                    break;
                }
                case "/pr":
                {
                    DbRepo repo = new DbRepo();
                    var stats = repo.GetPenaltiesRatioStats();

                    string statsDescription = stats.Aggregate("Statistiche sulle multe:\n",
                        (current, stat) => current + $"{stat.Key}: {stat.Value:P}\n");

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: statsDescription,
                        cancellationToken: cancellationToken);

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
                case "/rrr":
                {
                    var r = new DbRepo().GetRandom(100);
                    string text = string.Join("\n", r.Select(x => x.ToString()));
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Ecco 100 avemanie per te. Ora vai a leggerle sulla carreggiata. \n" + text,
                        cancellationToken: cancellationToken);
                    break;
                }
                case "/db":
                {
                    await SendDatabaseFile(botClient, cancellationToken, chatId);
                    break;
                }
                case "/v":
                {
                    string assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString() ??
                                             "Version not available";
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"L'attuale versione è: {assemblyVersion}",
                        cancellationToken: cancellationToken);
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
                              "/r - Restituisce 3 avemanie a caso\n" +
                              "/rrr - Restituisce 100 avemanie a caso\n" +
                              "/v - Numero di versione\n" +
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

    private static void ExecuteSQLCode(string messageText)
    {
        new DbRepo().Execute(messageText);
    }

    private static async Task ShowPenalties(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId)
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
    /// <param name="originalAveManiaId">The identifier of the original message entry in the database.</param>
    /// <returns>A task representing the asynchronous operation of sending the notification message.</returns>
    private static async Task<string> SendPenaltyMessage(ITelegramBotClient botClient,
        CancellationToken cancellationToken,
        long chatId, string senderName, string messageText, DbRepo repo, [DisallowNull] int? originalAveManiaId)
    {
        AveMania? am = repo.Find(originalAveManiaId.Value);

        string text =
            $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {am?.Author} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f";

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken).ConfigureAwait(false);


        return text;
    }

    private static async Task ShowActivity(ITelegramBotClient botClient, CancellationToken cancellationToken,
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

    static async Task<DateTime?> BanChatMember(ITelegramBotClient botClient, long chatId, long? userId,
        CancellationToken cancellationToken)
    {
        Random random = new Random();
        int randomNumber = random.Next(2, 8); // Generates a random integer between 1 and 7 inclusive.
        var banDate = DateTime.Now.AddDays(randomNumber);

        if (userId != null)
        {
            await botClient.RestrictChatMember(
                chatId: chatId,
                userId: userId.Value,
                permissions: new ChatPermissions
                {
                    CanSendMessages = false, // User can't send messages
                    CanSendPolls = false,
                    CanSendOtherMessages = false,
                    CanAddWebPagePreviews = false,
                    CanChangeInfo = false,
                    CanInviteUsers = false,
                    CanPinMessages = false
                }, false, banDate, cancellationToken);

            return banDate;
        }

        return null;
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}