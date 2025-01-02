using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        Console.WriteLine($"Bot {me.Username} is up and running!");

        Console.ReadLine();
        await cts.CancelAsync(); // Stop the bot when the program exits
    }

    private static async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
        await botClient.SendMessage(
            chatId: chatId,
            text: $"Hello, {senderName} said: {messageText}",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// TODO 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="chatId"></param>
    /// <param name="messageText"></param>
    private static async Task HandlePrivateMessage(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, string messageText)
    {
        // Private chat message
        await botClient.SendMessage(
            chatId: chatId,
            text: $"You said: {messageText}",
            cancellationToken: cancellationToken);
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}