using AveManiaBot.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AveManiaBot;

public class Botanna(ITelegramBotClient botClient, ILogger<Botanna> logger, IMessageHandler messageHandler) : BackgroundService
{
    private Timer? _periodicTimer;

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

        // Avvio del timer per l'invio periodico dell'ora
        StartPeriodicTimeMessage(stoppingToken);

        // Mantiene il servizio in esecuzione
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void StartPeriodicTimeMessage(CancellationToken cancellationToken)
    {
        // Timer che si attiva ogni 10 minuti (600000 millisecondi)
        _periodicTimer = new Timer(async _ => await SendTimeMessage(cancellationToken),
            null,
            TimeSpan.Zero, // Delay iniziale (invia subito il primo messaggio)
            TimeSpan.FromMinutes(10)); // Intervallo di 10 minuti

        logger.LogInformation("Timer per messaggi periodici dell'ora avviato (ogni 10 minuti)");
    }

    private async Task SendTimeMessage(CancellationToken cancellationToken)
    {
        try
        {
            var currentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            await botClient.SendTextMessageAsync(
                chatId: AmConstants.AmChatId,
                text: $"🕐 Ora attuale: {currentTime}",
                cancellationToken: cancellationToken);

            logger.LogInformation("Messaggio dell'ora inviato: {Time}", currentTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore nell'invio del messaggio periodico dell'ora");
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

    public override void Dispose()
    {
        _periodicTimer?.Dispose();
        base.Dispose();
    }
}