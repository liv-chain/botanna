using System.Diagnostics.CodeAnalysis;
using AveManiaBot.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AveManiaBot;

public class MessageHelper
{
    public static async Task<bool> CheckActivityArrest(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId,
        long? userId, string senderName, DbRepo repo)
    {
        var limit = 3;
        (bool hasExceeded, int count, DateTime? dt) checkResult = repo.HasAuthorExceededLimit(senderName, limit);
        Console.WriteLine($"Activity exceeded: {checkResult.hasExceeded} - activity count {checkResult.count}");
        switch (checkResult.hasExceeded)
        {
            case true when checkResult.count > limit + 1:
            {
                (DateTime? banDate, int days) = await BanChatMember(botClient, chatId, userId, cancellationToken);
                if (banDate.HasValue)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text:
                        $"{AmConstants.MalePoliceEmoji} ARRESTO: {senderName} sarà in prigione per {days} giorni fino al {banDate.Value:g} {AmConstants.MalePoliceEmoji}",
                        cancellationToken: cancellationToken);
                }

                return true;
            }
            case true:
                await RemarkUser(botClient, cancellationToken, chatId, senderName, checkResult.dt);
                break;
        }

        return false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    static async Task<(DateTime? banDate, int randomNumber)> BanChatMember(ITelegramBotClient botClient, long chatId, long? userId,
        CancellationToken cancellationToken)
    {
        Random random = new Random();
        int randomNumber = random.Next(2, 10);
        var banDate = DateTime.Now.AddDays(randomNumber);

        if (userId != null)
        {
            try
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new PorcodioGliAdminException($"Non è stato possibile arrestare l'utente: {e.Message}", banDate, randomNumber);
            }

            return (banDate, randomNumber);
        }

        return (null, 0);
    }

    public static async Task<string> SendPenaltyMessage(ITelegramBotClient botClient, CancellationToken cancellationToken, string senderName, string messageText, DbRepo repo, [DisallowNull] int? originalAveManiaId)
    {
        AveMania? am = repo.Find(originalAveManiaId.Value);

        string text =
            $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {am?.Author} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f";

        await botClient.SendMessage(
            chatId: AmConstants.AmChatId,
            text,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return text;
    }

    private static async Task RemarkUser(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId,
        string senderName, DateTime? dt)
    {
        string timeMsg = string.Empty;
        if (dt != null)
        {
            timeMsg = $" Potrai riprendere a scrivere senza incorrere in arresto alle {dt.Value:t} ";
        }

        await botClient.SendMessage(
            chatId: chatId,
            text:
            $"{senderName}, {GetRandomRemark()} Al prossimo richiamo di oggi scatterà l'arresto.{timeMsg}{AmConstants.MalePoliceEmoji}",
            cancellationToken: cancellationToken);
    }

    private static string GetRandomRemark()
    {
        Random random = new Random();
        int index = random.Next(AmConstants.Remarks.Count);
        return AmConstants.Remarks[index];
    }
    
    public static async Task CheckPenaltyArrest(ITelegramBotClient botClient, CancellationToken cancellationToken,
        long chatId, long? userId, string senderName, DbRepo repo)
    {
        (bool hasExceeded, int count) checkPenalResult = repo.HasAuthorExceededPenalLimit(senderName);
        Console.WriteLine($"Penalties exceeded: {checkPenalResult.hasExceeded} - penalties count {checkPenalResult.count}");
        if (checkPenalResult.hasExceeded)
        {
            (DateTime? banDate, int days) = await BanChatMember(botClient, chatId, userId, cancellationToken);
            if (banDate.HasValue)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text:
                    $"{AmConstants.MalePoliceEmoji} ARRESTO per eccesso di multe: {senderName} sarà in prigione per {days} giorni fino al {banDate.Value:g} {AmConstants.MalePoliceEmoji}",
                    cancellationToken: cancellationToken);
            }
        }
    }
}