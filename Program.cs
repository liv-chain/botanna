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

    static async Task Main(string[] args)
    {
        await RunBot();
        Console.ReadLine();
    }

    private static async Task RestartBot()
    {
        Console.WriteLine("Restarting bot...");
        if (_botClient != null) await _botClient.Close();
        await RunBot();
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
            Console.WriteLine($"Bot {me.Username} version {assemblyVersion} is up and running!");
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
    /// Handles the incoming update from the Telegram bot, processing edited messages, messages in specific chat types,
    /// and determining the appropriate action or response for the update.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="update">The update object containing the details of the changes or actions that occurred, such as new or edited messages.</param>
    /// <param name="cancellationToken">Token used to propagate notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation for processing the incoming update.</returns>
    private static async Task HandleUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.EditedMessage != null)
        {
            var chatType = update.EditedMessage.Chat.Type;
            if (chatType != ChatType.Group && chatType != ChatType.Supergroup)
            {
                return;
            }
            
            Message? edited = update.EditedMessage;
            var newText = edited.Text ?? "";

            // edit di am già scritte
            if (Helpers.IsAveMania(newText))
            {
                var repo = new DbRepo();
                var originalText = repo.GetOriginalText(edited.MessageId); 
                string senderName = $"{edited.From?.FirstName} {edited.From?.LastName}".Trim();
                
                await botClient.SendMessage(
                     chatId: AmConstants.AmChatId,
                     text: $"{AmConstants.AlertEmoji} {senderName} ha aggiornato {originalText} in {newText}",
                     cancellationToken: cancellationToken);

                var id = repo.CheckPenalty(newText);
                var existing = id != null;
                if (existing)
                {
                    await MessageHelper.SendPenaltyMessage(botClient, cancellationToken, senderName, newText, repo, id!);
                }
                
                await repo.Update(edited.MessageId, newText);
            }
            return;
        }

        //The pattern checks whether is an **object** and whether its property is **not null** or empty. If exists and is valid, the value is extracted into the variable.
        //`update.Message``Text``Text``messageText`
        if (update.Message is not { Text: { } messageText } message)
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
        var messageId = message.MessageId;
        var messageDateTime = message.Date;

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
                await new MessageHandler(botClient).HandlePrivateMessage(cancellationToken, chatId, messageText);
                break;
        }
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}