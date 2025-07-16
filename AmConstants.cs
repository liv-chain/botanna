namespace AveManiaBot;

public static class AmConstants
{
    public const string BotToken = "7997826290:AAGdFuQNlwjynaheYTV6wq7kBlYr5WBWNQw"; // ufficiale
    public const string DbPath = "ave_mania.db";
    public const long AmChatId = -1002381222429;

    //public const string BotToken = "7539070202:AAGO7hmQepJ9BIcDRdclkBTsEMpKtzYKnrw"; // test
    //public const string DbPath = "ave_mania__.db"; // test
//    public const long AmChatId = -1002880316353; // test 
    public const string AlertEmoji = "\u26A0\uFE0F"; // ⚠️
    public const string MalePoliceEmoji = "\U0001F46E\u200D\u2642\U0000FE0F"; // 👮‍♂️
    public const string FemalePoliceEmoji = "\U0001F46E\u200D\u2640\uFE0F"; // 👮‍♀️
    public const string YellowCardEmoji = "\U0001F7E8"; // 🟨
    public const string PoliceCarEmoji = "\U0001F693"; // 🚓
    public const string ConnectionString = $"Data Source={DbPath};Version=3;";

    public const int
        ActivityWarningLimit = 3; // il numero massimo di am che si possono scrivere senza ricevere un richiamo

    public const int PenaltyLimit = 3;
    public const int ActivityTimeSpanHours = 12; // Number of hours to check for author exceeding limit
    public const int PenaltyHoursTimeSpan = 36; // Number of hours to check for author exceeding limit

    public static readonly List<string> Remarks =
    [
        YellowCardEmoji,
        // "messo il turbo oggi, eh?",
        // "sei un podcast vivente ma hai rotto il cazzo.",
        // "stai facendo il monologo finale di un film, o c’è una pausa da qualche parte?",
        // "vuoi un microfono, o ti senti già abbastanza amplificato?",
        // "sei già al capitolo 3 del tuo libro di puttanate?",
        // "minchia oh, non ci sei solo tu qua eh.",
        // "vai a scavare buche nel Tagliamento.",
        "merda secca per te.",
        "sei un tonno",
        "sei una trota",
        "oooooooh",
        // "forse è il momento di passare la parola agli altri.",
        // "vai a giocare con la merda nella tundra.",
        "palettaaaaaaaaaaaaaaaaa!",
        // "mi piacerebbe sentire anche il punto di vista di qualcun altro di voi stronzetti.",
        // "grazie per la tua passione, ma non hai un cazzo da fare oggi?",
        // "favorisci il libretto di circolazione che ci cago dentro.",
        "beviti la benzina invece di circolare.",
        // "puoi riassumere? Abbiamo poco tempo per leggere tutte le cagate che scrivi.",
        // "hai coperto ogni dettaglio delle tue minchiate, possiamo passare al prossimo argomento?",
        // "facciamo un break dalle minchiate, che dici?",
        // "va bene, ho capito. Possiamo chiudere il discorso qui che hai scardinato lo scroto?",
        // "ma smettila di fare il gazzabbubbo di turno, che qui non siamo al circo!",
        // "se continui a parlare così, finisci dritto dritto nel manuale del perfetto spruzzafuffa.",
        // "ma sei proprio un mestolone di gorgoglione oggi, eh?",
        // "ti sgorfo negli occhi",
        // "oh, gazzabbubbo ufficiale, la parola la passiamo anche agli altri o no?",
        // "sembri un frastugliacazzi, vai avanti all’infinito!",
        // "basta con questa manfrina da scatafasco ambulante!",
        // "ma quanto hai bevuto dal calderone della logorrina oggi?",
        // "Hai intenzione di brevettare questa infinita marea di cagate?",
        // "Va bene, ho capito, il mondo ruota intorno alla tua voce sgradevole oggi!",
        // "Non ti stanchi mai di sentire il suono delle tue stesse stronzate?",
        // "Aspetta, fammi respirare tra una frase e l’altra minchia!",
        // "Se parlassi un po’ di meno, potremmo forse risolvere questo problema prima di domani.",
        // "Se le parole fossero denaro, potresti comprarti un biglietto per andare affanculo.",
        // "Mi fai sentire come se stessi leggendo il manuale di un elettrodomestico.",
        // "Hai già conquistato il trofeo del scassacazzi dell’anno, possiamo passare oltre?"
    ];
}