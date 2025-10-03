namespace AveManiaBot;

public static class AmConstants
{
    public const string BotToken = "7997826290:AAGdFuQNlwjynaheYTV6wq7kBlYr5WBWNQw"; // ufficiale
    public const string DbPath = "ave_mania.db";
    public const long AmChatId = -1002381222429;

    //public const string BotToken = "7539070202:AAGO7hmQepJ9BIcDRdclkBTsEMpKtzYKnrw"; // test
    //public const string DbPath = "ave_mania.db"; // test
    //public const long AmChatId = -1002880316353; // test 
    
    public const string AlertEmoji = "\u26A0\uFE0F"; // ⚠️
    public const string PenEmoji = "\U0001F58A\uFE0F"; // 🖊️
    public const string MalePoliceEmoji = "\U0001F46E\u200D\u2642\U0000FE0F"; // 👮‍♂️
    public const string FemalePoliceEmoji = "\U0001F46E\u200D\u2640\uFE0F"; // 👮‍♀️
    public const string YellowCardEmoji = "\U0001F7E8"; // 🟨
    public const string PoliceCarEmoji = "\U0001F693"; // 🚓
    public const string ConnectionString = $"Data Source={DbPath};Version=3;";

    public const int ActivityWarningLimit = 3; // il numero massimo di am che si possono scrivere senza ricevere un richiamo

    public const int PenaltyLimit = 3;
    public const int ActivityTimeSpanHours = 12; // Number of hours to check for author exceeding limit
    public const int PenaltyHoursTimeSpan = 48; // Number of hours to check for author exceeding limit
    public const int MaxBotannaRequests = 2;

    public static class HandEmojis
    {
        public const string ThumbsUp = "👍";
        public const string ThumbsDown = "👎";
        public const string ClappingHands = "👏";
        public const string RaisingHands = "🙌";
        public const string FoldedHands = "🙏";
        public const string OpenHands = "👐";
        public const string OkHand = "👌";
        public const string VictoryHand = "✌️";
        public const string CallMeHand = "🤙";
        public const string FlexedBiceps = "💪";
        public const string WavingHand = "👋";
        public const string BackhandIndexPointingLeft = "👈";
        public const string BackhandIndexPointingRight = "👉";
        public const string BackhandIndexPointingUp = "👆";
        public const string BackhandIndexPointingDown = "👇";
        public const string IndexPointingUp = "☝️";
        public const string RaisedHand = "✋";
        public const string HandWithFingersSplayed = "🖐️";
        public const string VulcanSalute = "🖖";
        public const string WritingHand = "✍️";
        public const string PinchingHand = "🤏";
        public const string LoveYouGesture = "🤟";
        public const string CrossedFingers = "🤞";
        public const string PalmsUpTogether = "🤲";
        public const string Handshake = "🤝";
    }

    
    public static readonly List<string> Remarks =
    [
        YellowCardEmoji,
        "sei in modalità vomitacazzate avanzata, eh?",
        "ogni tua parola è un insulto al silenzio",
        "stai facendo il doppiaggio del nulla cosmico?",
        "hai intenzione di mollare il microfono o ti serve un gancio?",
        "parli come se stessi cercando di perdere amici a tempo record",
        "ti hanno detto che il silenzio è d’oro? Tu sei in debito",
        "c'è più senso in un rutto che in quello che dici",
        "ti puzza il culo",
        "hai un talento raro: riesci a parlare tanto dicendo PATATE",
        "stai trasformando l’aria in peti, complimenti",
        "hai finito di scaccolarti con la lingua o vai avanti?",
        "non so se stai parlando o cercando di evocare un demone",
        "se c’era un punto, l’hai seppellito sotto dieci strati di cagate",
        "stai tenendo un TED Talk di minchiate senza pubblico né invito",
        "le tue parole sono come l’umidità: fastidiose e ovunque",
        "c'è un limite a tutto… tranne che alla tua voce, a quanto pare",
        @"sei il trailer ufficiale de 'L Invasione dei Logorroici'",
        "stai leggendo il copione di una telenovela mentale?",
        "non è che se urli di più diventi più interessante",
        "se ci fosse un campionato mondiale di logorrea, saresti il CT",
        "più parli, più Google si deindicizza da solo",
        "il tuo flusso di coscienza è in realtà uno tsunami di cazzate",
        "sta diventando uno spettacolo di cabaret… senza pubblico e senza talento",
        "sei riuscito a far addormentare persino il sarcasmo",
        "ogni tuo intervento è un attentato alla pazienza collettiva",
        "hai intenzione di parlare anche con le sedie o ti fermi agli umani?",
        "se il tuo cervello avesse una tastiera, sarebbe bloccato su CAPS LOCK",
        "sei la versione umana del buffering infinito",
        "ti infilo la paletta nel culo",
        "messo il turbo oggi, eh?",
        "sei un podcast vivente ma hai rotto il cazzo",
        "stai facendo il monologo finale di un film, o c’è una pausa da qualche parte?",
        "vuoi un microfono, o ti senti già abbastanza amplificato?",
        "sei già al capitolo 3 del tuo libro di puttanate?",
        "minchia oh, non ci sei solo tu qua eh",
        "vai a scavare buche nel Tagliamento",
        "merda secca per te",
        "sei un tonno",
        "sei una trota",
        "oooooooh",
        "forse è il momento di passare la parola agli altri",
        "vai a giocare con la merda nella tundra",
        "palettaaaaaaaaaaaaaa!",
        "mi piacerebbe sentire anche il punto di vista di qualcun altro di voi stronzetti",
        "grazie per la tua passione, ma non hai un cazzo da fare oggi?",
        "favorisci il libretto di circolazione che ci cago dentro",
        "beviti la benzina invece di circolare",
        "puoi riassumere? Abbiamo poco tempo per leggere tutte le cagate che scrivi",
        "hai coperto ogni dettaglio delle tue minchiate, possiamo passare al prossimo argomento?",
        "facciamo un break dalle minchiate, che dici?",
        "va bene, ho capito. Possiamo chiudere il discorso qui che hai scardinato lo scroto?",
        "ma smettila di fare il gazzabbubbo di turno, che qui non siamo al circo!",
        "se continui a parlare così, finisci dritto dritto nel manuale del perfetto spruzzafuffa",
        "Non ti stanchi mai di sentire il suono delle tue stesse stronzate?",
        "Aspetta, fammi respirare tra una frase e l’altra minchia!",
        "Se le parole fossero denaro, potresti comprarti un biglietto per andare affanculo",
        "Mi fai sentire come se stessi leggendo il manuale di un elettrodomestico",
        "Hai già conquistato il trofeo del scassacazzi dell’anno, possiamo passare oltre?",
        "hai aperto bocca e si è suicidato un neurone",
        "sei il riassunto parlato di un errore di sistema",
        "il tuo discorso è il reboot inutile di un film già brutto",
        "non è che se metti enfasi diventa contenuto, eh",
        "stai facendo stand-up comedy, ma il pubblico sta scappando",
        "hai il dono dell’ubriacatura verbale",
        "hai messo in loop l’eco della tua opinione",
        "se ti interrompessi, salverei l’umanità da almeno altri 5 minuti di disagio",
        "stai abusando del nostro tempo come fosse una discarica",
        "parli come se stessi tentando di depennare l'intelligenza dalla stanza",
        "hai costruito una cattedrale di parole su fondamenta di fango",
        "sei un podcast che nessuno ha chiesto e nessuno può mettere in pausa",
        "più parli e più l’universo si pente di averti dato voce",
        "stai facendo il karaoke delle stronzate",
        "hai appena vinto il premio 'fiato sprecato 2025'",
        "sei un generatore automatico di delirio logorroico",
        "ogni tua frase è un calcio negli zebedei del buon senso",
        "sei riuscito a dire tutto e niente in un unico fiato tossico",
        "ma uno stop, una virgola, un senso… lo vogliamo mettere o no?"
    ];

    
}