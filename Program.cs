using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace AveManiaBot;

class Program
{
    static async Task Main(string[] _)
    {
        // Configurazione dell'host con dependency injection
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configurazione del bot client come singleton
                services.AddSingleton<ITelegramBotClient, TelegramBotClient>(sp => new TelegramBotClient(AmConstants.BotToken));
                services.AddHostedService<Botanna>();
                
                // Registrazione di altri servizi - pass connection string from AmConstants
                services.AddSingleton<IDbRepo, DbRepo>();
                services.AddSingleton<IMessageHandler, BotannaMessageHandler>();
                
                // Configurazione del servizio di hosting del bot
                services.AddSingleton<IDbConnectionFactory>(provider => new DbConnectionFactory(AmConstants.ConnectionString));
                services.AddScoped<IDbRepo, DbRepo>();

            })
            .Build();

        // Avvia l'host
        await host.RunAsync();
    }
}