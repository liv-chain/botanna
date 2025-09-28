using AveManiaBot.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AveManiaBot;

public class TelegramBotService(
    ITelegramBotClient botClient,
    ILogger<TelegramBotService> logger,
    MessageHandler messageHandler)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Configurazione delle opzioni di ricezione
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [] // Receive all update types
        };

        // Avvio della ricezione degli update
        botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationToken: stoppingToken);

        try
        {
            // Verifica del bot
            var me = await botClient.GetMe(cancellationToken: stoppingToken);
            string assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Version not available";
            logger.LogInformation("{DateTime} Bot {Username} version {Version} is up and running!",
                DateTime.Now, me.Username, assemblyVersion);
        }
        catch (Exception ex1)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) =>
                    {
                        logger.LogWarning("Retry {RetryCount} for GetMe due to: {ExceptionMessage}. Waiting {Seconds} seconds before retrying...",
                            retryCount, exception.Message, timeSpan.TotalSeconds);
                    });

            try
            {
                var me = await retryPolicy.ExecuteAsync(() => botClient.GetMe(cancellationToken: stoppingToken));
                logger.LogInformation("{DateTime} Bot {Username} is up and running!", DateTime.Now, me.Username);
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "Failed to retrieve bot information");
            }
        }
    }

    private async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Logica di HandleUpdate simile al codice originale, 
        // ma utilizzando _messageHandler iniettato
        logger.LogInformation("Update type: {UpdateType}", update.Type);

        if (update.Message is not { Text: { } messageText } message)
            return;

        var chatType = message.Chat.Type;
        var chatId = message.Chat.Id;
        var userId = message.From?.Id;
        string senderName = $"{message.From?.FirstName} {message.From?.LastName}".Trim();

        try
        {
            switch (chatType)
            {
                case ChatType.Group:
                case ChatType.Supergroup:
                    await messageHandler.HandleGroupMessage(
                        cancellationToken,
                        chatId,
                        userId,
                        senderName,
                        messageText,
                        message.MessageId,
                        message.Date.ToLocalTime());
                    break;
                default:
                    await messageHandler.HandlePrivateMessage(
                        cancellationToken,
                        chatId,
                        messageText,
                        senderName);
                    break;
            }
        }
        catch (PorcodioGliAdminException e)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"{AmConstants.MalePoliceEmoji} Onta morale per il gran visir {senderName}. " +
                      $"Sarà tenuto a non scrivere nulla per {e.Days} giorni fino al {e.BanDate:g} {AmConstants.MalePoliceEmoji}",
                cancellationToken: cancellationToken);
        }
    }

    private Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An error occurred");
        return Task.CompletedTask;
    }
}