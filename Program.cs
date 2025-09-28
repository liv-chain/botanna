using AveManiaBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

class Program
{
    static async Task Main(string[] _)
    {
        // Configurazione dell'host con dependency injection
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configurazione del bot client come singleton
                services.AddSingleton<ITelegramBotClient>(sp => 
                    new TelegramBotClient(AmConstants.BotToken));

                // Registrazione di altri servizi
                services.AddSingleton<DbRepo>();
                services.AddSingleton<MessageHandler>();
                
                // Configurazione del servizio di hosting del bot
                services.AddHostedService<TelegramBotService>();
            })
            .Build();

        // Avvia l'host
        await host.RunAsync();
    }
}

