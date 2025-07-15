using System.Diagnostics.CodeAnalysis;
using AveManiaBot.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AveManiaBot;

public class MessageHelper
{
    public static (bool hasExceeded, int count, DateTime?) CheckActivityArrest(string senderName, DbRepo repo, DateTime messageDateTime)
    {
        return repo.HasAuthorExceededLimit(senderName, AmConstants.ActivityWarningLimit, messageDateTime);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="botClient"></param>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="banDate"></param>
    /// <param name="days"></param>
    /// <returns></returns>
    public static async Task BanChatMember(ITelegramBotClient botClient, long chatId, long? userId,
        CancellationToken cancellationToken, DateTime banDate, int days)
    {
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
                throw new PorcodioGliAdminException($"Non è stato possibile arrestare l'utente: {e.Message}", banDate, days);
            }
        }
    }

    public static async Task<string> SendPenaltyMessage(ITelegramBotClient botClient, CancellationToken cancellationToken, string senderName, string messageText, DbRepo repo,
        [DisallowNull] int? originalAveManiaId)
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

    public static async Task RemarkUser(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId,
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

    public static async Task<(bool hasExceeded, int count)> CheckPenaltyArrest(string senderName, DbRepo repo, DateTime messageDateTime)
    {
        (bool hasExceeded, int count) checkPenalResult = repo.HasAuthorExceededPenalLimit(senderName, messageDateTime);
        Console.WriteLine($"Penalties exceeded: {checkPenalResult.hasExceeded} - penalties count {checkPenalResult.count}");
        return checkPenalResult;
    }
}