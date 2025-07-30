using AveManiaBot;
using AveManiaBot.Exceptions;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

// ReSharper disable once CheckNamespace
class Program
{
    private static TelegramBotClient? _botClient;

    static async Task Main(string[] _)
    {
        await RunBot();
        Console.ReadLine();
        while (true)
        {
            Thread.Sleep(1000);
        }

    }

    private static async Task RunBot()
    {
        _botClient = new TelegramBotClient(AmConstants.BotToken);

        // Start receiving updates
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [] // Receive all update types
        };

        new DbRepo().CheckPenalty("diocannone");

        _botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationToken: cts.Token);

        try
        {
            var me = await _botClient.GetMe(cancellationToken: cts.Token);
            string assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Version not available";
            Console.WriteLine($"{DateTime.Now:u} Bot {me.Username} version {assemblyVersion} is up and running!");
        }
        catch (Exception)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) =>
                    {
                        Console.WriteLine(
                            $"Retry {retryCount} for GetMe due to: {exception.Message}. Waiting {timeSpan.TotalSeconds} seconds before retrying...");
                    });
            try
            {
                var me = await retryPolicy.ExecuteAsync(() => _botClient.GetMe(cancellationToken: cts.Token));
                Console.WriteLine($"{DateTime.Now:u} Bot {me.Username} is up and running!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:u} Failed to retrieve bot information: {ex.Message}");
            }
        }

        Console.ReadLine();
        await cts.CancelAsync(); // Stop the bot when the program exits
    }


    /// <summary>
    /// Handles the incoming update from the Telegram bot, processing edited messages, messages in specific chat types,
    /// and determining the appropriate action or response for the update.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="update">The update object containing the details of the changes or actions that occurred, such as new or edited messages.</param>
    /// <param name="cancellationToken">Token used to propagate notification that the operation should be canceled.</param>
    /// <returns>A task representing the asynchronous operation for processing the incoming update.</returns>
    private static async Task HandleUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{DateTime.Now:u} Update type: " + update.Type);
        
        if (update.EditedMessage != null)
        {
            var chatType = update.EditedMessage.Chat.Type;
            var chatId = update.EditedMessage.Chat.Id;
            var userId = update.EditedMessage.From?.Id;
            var messageDateTime = update.EditedMessage.Date.ToLocalTime();

            if (chatType != ChatType.Group && chatType != ChatType.Supergroup)
            {
                return;
            }

            Message? edited = update.EditedMessage;
            await HandleEdit(botClient, cancellationToken, edited, messageDateTime, chatId, userId);
            return;
        }
        
        if (update.Message is not { Text: { } messageText } message)
            return;

        // Usual flow
        await ProcessMessageBasedOnChatType(botClient, cancellationToken, message, messageText);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="edited"></param>
    /// <param name="messageDateTime"></param>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    private static async Task HandleEdit(ITelegramBotClient botClient, CancellationToken cancellationToken, Message edited, DateTime messageDateTime, long chatId,
        long? userId)
    {
        var newText = edited.Text ?? String.Empty;
        if (!Helpers.IsAveMania(newText)) return;

        Console.WriteLine($"{DateTime.Now:u} Message edited by {edited.From?.FirstName} {edited.From?.LastName}: {newText}");

        // edit di am già scritte
        var repo = new DbRepo();
        var originalText = repo.GetOriginalText(edited.MessageId);
        string senderName = $"{edited.From?.FirstName} {edited.From?.LastName}".Trim();

        if (string.IsNullOrEmpty(originalText)) return;
        if (originalText == newText) return;

        await botClient.SendMessage(
            chatId: AmConstants.AmChatId,
            text: $"{AmConstants.PenEmoji} {senderName} ha aggiornato {originalText} in {newText}",
            cancellationToken: cancellationToken);

        // todo_1 gestione edit e arresti
        // var activityCheck = MessageHelper.CheckActivityArrest(senderName, repo, messageDateTime);
        //
        // switch (activityCheck.hasExceeded)
        // {
        //     case true when activityCheck.count > AmConstants.ActivityWarningLimit + 1:
        //     {
        //         var days = Math.Min(AmConstants.ActivityTimeSpanHours - (int)Math.Ceiling(activityCheck.timeSpan), 10);
        //         var banDate = DateTime.Now.AddDays(days);
        //         await botClient.SendMessage(
        //             chatId: chatId,
        //             text:
        //             $"{AmConstants.MalePoliceEmoji} ARRESTO: {senderName} sarà in prigione per {days} giorni fino al {banDate:g} {AmConstants.FemalePoliceEmoji}",
        //             cancellationToken: cancellationToken);
        //
        //         break;
        //     }
        //     case true:
        //         await MessageHelper.RemarkUser(botClient, cancellationToken, chatId, senderName, activityCheck.date);
        //         break;
        // }
        //
        var id = repo.CheckPenalty(newText);
        var existing = id != null;
        if (existing)
        {
            Random random = new Random();
            int randomDays = 0;

            var text = await MessageHelper.SendPenaltyMessage(botClient, cancellationToken, senderName, newText, repo, id!);
            repo.Insert(new Penalty(text, senderName, 0, DateTime.Now));

            var checkPenalResult = await MessageHelper.CheckPenaltyArrest(senderName, repo, messageDateTime);
            if (checkPenalResult.hasExceeded)
            {
                randomDays += random.Next(2, 11);
                DateTime banDate = DateTime.Now.AddDays(randomDays);
                await botClient.SendMessage(
                    chatId: chatId,
                    text:
                    $"{AmConstants.PoliceCarEmoji} ARRESTO per eccesso di multe: {senderName} sarà in prigione per {randomDays} giorni fino al {banDate:g} {AmConstants.MalePoliceEmoji}",
                    cancellationToken: cancellationToken);

                await MessageHelper.BanChatMember(botClient, chatId, userId, cancellationToken, banDate, randomDays);
            }
        }

        await repo.Update(edited.MessageId, newText);
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
        var messageId = message.MessageId;
        var messageDateTime = message.Date.ToLocalTime();

        if (!string.IsNullOrEmpty(message.From?.LastName))
            senderName += $" {message.From?.LastName}";

        switch (chatType)
        {
            case ChatType.Group:
            case ChatType.Supergroup:
                try
                {
                    await new MessageHandler(botClient).HandleGroupMessage(cancellationToken, chatId, userId, senderName, messageText, messageId, messageDateTime);
                }
                catch (PorcodioGliAdminException e)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text:
                        $"{AmConstants.MalePoliceEmoji} Onta morale per il gran visir {senderName}. Sarà tenuto a non scrivere nulla per " +
                        $"{e.Days} giorni fino al {e.BanDate:g} {AmConstants.MalePoliceEmoji}",
                        cancellationToken: cancellationToken);
                }

                break;
            default:
                await new MessageHandler(botClient).HandlePrivateMessage(cancellationToken, chatId, messageText, senderName);
                break;
        }
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{DateTime.Now:u} Error: {exception.Message}");
        return Task.CompletedTask;
    }
    
}