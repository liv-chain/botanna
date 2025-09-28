using System;

namespace AveManiaBot.Model
{
    /// <summary>
    /// Rappresenta un utente di una chat Telegram
    /// </summary>
    public class TelegramUser
    {
        /// <summary>
        /// ID univoco del database
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ID univoco Telegram dell'utente
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Nome utente Telegram
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Nome completo dell'utente
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Lingua preferita dell'utente
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Genere dell'utente
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Data di registrazione dell'utente
        /// </summary>
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ultimo accesso al bot
        /// </summary>
        public DateTime? LastAccessDate { get; set; }

        /// <summary>
        /// Flag per indicare se l'utente è attivo
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Ruolo dell'utente nel sistema
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Aggiorna l'ultimo accesso dell'utente
        /// </summary>
        public void UpdateLastAccess()
        {
            LastAccessDate = DateTime.UtcNow;
        }
    }
}
