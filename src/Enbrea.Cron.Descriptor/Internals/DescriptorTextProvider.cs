#region Enbrea.Cron - Copyright (c) STÜBER SYSTEMS GmbH
/*    
 *    Enbrea.Cron 
 *    
 *    Copyright (c) STÜBER SYSTEMS GmbH
 *
 *    Licensed under the MIT License.
 */
#endregion

using System.Globalization;

namespace Enbrea.Cron.Descriptor;

/// <summary>
/// Provides localized text templates and culture resolution for schedule and relative-time descriptor output.
/// </summary>
/// <remarks>
/// Supports predefined language resources and falls back to English when a language or resource key is unavailable.
/// </remarks>
internal static class DescriptorTextProvider
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _languages =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "every minute",
                ["Expression_EveryHourAtMinute"] = "every hour at minute {0}",
                ["Expression_EveryDayAtTime"] = "every day at {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "every month on day {0} at {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "every year on {0} {1} at {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "every week on {0} at {1}",
                ["Expression_WeekdaysAtTime"] = "every weekday at {0}",
                ["Expression_WeekendsAtTime"] = "every weekend day at {0}",
                ["Expression_Generic"] = "schedule: {0}",
                ["Schedule_TimeZone"] = "Time zone: {0}",
                ["Schedule_NextOccurrence"] = "next occurrence {0}",
                ["Schedule_PreviousOccurrence"] = "previous occurrence {0}",
                ["Schedule_NoNextOccurrence"] = "no next occurrence in the selected interval",
                ["Schedule_NoPreviousOccurrence"] = "no previous occurrence in the selected interval",
                ["Relative_Now"] = "now",
                ["Relative_In"] = "in {0}",
                ["Relative_Ago"] = "{0} ago",
                ["Relative_Days"] = "{0} day(s)",
                ["Relative_Hours"] = "{0} hour(s)",
                ["Relative_Minutes"] = "{0} minute(s)",
                ["Relative_Seconds"] = "{0} second(s)"
            },
            ["de"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "jede Minute",
                ["Expression_EveryHourAtMinute"] = "jede Stunde in Minute {0}",
                ["Expression_EveryDayAtTime"] = "jeden Tag um {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "jeden Monat am Tag {0} um {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "jedes Jahr am {1}. {0} um {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "jede Woche am {0} um {1}",
                ["Expression_WeekdaysAtTime"] = "jeden Werktag um {0}",
                ["Expression_WeekendsAtTime"] = "an jedem Wochenendtag um {0}",
                ["Expression_Generic"] = "Zeitplan: {0}",
                ["Schedule_TimeZone"] = "Zeitzone: {0}",
                ["Schedule_NextOccurrence"] = "nächstes Auftreten {0}",
                ["Schedule_PreviousOccurrence"] = "vorheriges Auftreten {0}",
                ["Schedule_NoNextOccurrence"] = "kein nächstes Auftreten im ausgewählten Intervall",
                ["Schedule_NoPreviousOccurrence"] = "kein vorheriges Auftreten im ausgewählten Intervall",
                ["Relative_Now"] = "jetzt",
                ["Relative_In"] = "in {0}",
                ["Relative_Ago"] = "vor {0}",
                ["Relative_Days"] = "{0} Tag(e)",
                ["Relative_Hours"] = "{0} Stunde(n)",
                ["Relative_Minutes"] = "{0} Minute(n)",
                ["Relative_Seconds"] = "{0} Sekunde(n)"
            },
            ["es"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "cada minuto",
                ["Expression_EveryHourAtMinute"] = "cada hora en el minuto {0}",
                ["Expression_EveryDayAtTime"] = "cada día a las {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "cada mes el día {0} a las {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "cada año el {1} de {0} a las {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "cada semana el {0} a las {1}",
                ["Expression_WeekdaysAtTime"] = "cada día laborable a las {0}",
                ["Expression_WeekendsAtTime"] = "cada día de fin de semana a las {0}",
                ["Expression_Generic"] = "programación: {0}",
                ["Schedule_TimeZone"] = "Zona horaria: {0}",
                ["Schedule_NextOccurrence"] = "próxima ocurrencia {0}",
                ["Schedule_PreviousOccurrence"] = "ocurrencia anterior {0}",
                ["Schedule_NoNextOccurrence"] = "ninguna ocurrencia siguiente en el intervalo seleccionado",
                ["Schedule_NoPreviousOccurrence"] = "ninguna ocurrencia anterior en el intervalo seleccionado",
                ["Relative_Now"] = "ahora",
                ["Relative_In"] = "en {0}",
                ["Relative_Ago"] = "hace {0}",
                ["Relative_Days"] = "{0} día(s)",
                ["Relative_Hours"] = "{0} hora(s)",
                ["Relative_Minutes"] = "{0} minuto(s)",
                ["Relative_Seconds"] = "{0} segundo(s)"
            },
            ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "chaque minute",
                ["Expression_EveryHourAtMinute"] = "chaque heure à la minute {0}",
                ["Expression_EveryDayAtTime"] = "chaque jour à {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "chaque mois le jour {0} à {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "chaque année le {1} {0} à {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "chaque semaine le {0} à {1}",
                ["Expression_WeekdaysAtTime"] = "chaque jour de semaine à {0}",
                ["Expression_WeekendsAtTime"] = "chaque jour de week-end à {0}",
                ["Expression_Generic"] = "planification : {0}",
                ["Schedule_TimeZone"] = "Fuseau horaire : {0}",
                ["Schedule_NextOccurrence"] = "prochaine occurrence {0}",
                ["Schedule_PreviousOccurrence"] = "occurrence précédente {0}",
                ["Schedule_NoNextOccurrence"] = "aucune occurrence suivante dans l'intervalle sélectionné",
                ["Schedule_NoPreviousOccurrence"] = "aucune occurrence précédente dans l'intervalle sélectionné",
                ["Relative_Now"] = "maintenant",
                ["Relative_In"] = "dans {0}",
                ["Relative_Ago"] = "il y a {0}",
                ["Relative_Days"] = "{0} jour(s)",
                ["Relative_Hours"] = "{0} heure(s)",
                ["Relative_Minutes"] = "{0} minute(s)",
                ["Relative_Seconds"] = "{0} seconde(s)"
            },
            ["it"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "ogni minuto",
                ["Expression_EveryHourAtMinute"] = "ogni ora al minuto {0}",
                ["Expression_EveryDayAtTime"] = "ogni giorno alle {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "ogni mese il giorno {0} alle {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "ogni anno il {1} {0} alle {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "ogni settimana il {0} alle {1}",
                ["Expression_WeekdaysAtTime"] = "ogni giorno feriale alle {0}",
                ["Expression_WeekendsAtTime"] = "ogni giorno festivo alle {0}",
                ["Expression_Generic"] = "pianificazione: {0}",
                ["Schedule_TimeZone"] = "Fuso orario: {0}",
                ["Schedule_NextOccurrence"] = "prossima occorrenza {0}",
                ["Schedule_PreviousOccurrence"] = "occorrenza precedente {0}",
                ["Schedule_NoNextOccurrence"] = "nessuna occorrenza successiva nell'intervallo selezionato",
                ["Schedule_NoPreviousOccurrence"] = "nessuna occorrenza precedente nell'intervallo selezionato",
                ["Relative_Now"] = "adesso",
                ["Relative_In"] = "tra {0}",
                ["Relative_Ago"] = "{0} fa",
                ["Relative_Days"] = "{0} giorno/i",
                ["Relative_Hours"] = "{0} ora/e",
                ["Relative_Minutes"] = "{0} minuto/i",
                ["Relative_Seconds"] = "{0} secondo/i"
            },
            ["pt"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "a cada minuto",
                ["Expression_EveryHourAtMinute"] = "a cada hora no minuto {0}",
                ["Expression_EveryDayAtTime"] = "todos os dias às {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "todos os meses no dia {0} às {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "todos os anos em {1} de {0} às {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "toda semana na {0} às {1}",
                ["Expression_WeekdaysAtTime"] = "todo dia útil às {0}",
                ["Expression_WeekendsAtTime"] = "todo fim de semana às {0}",
                ["Expression_Generic"] = "agendamento: {0}",
                ["Schedule_TimeZone"] = "Fuso horário: {0}",
                ["Schedule_NextOccurrence"] = "próxima ocorrência {0}",
                ["Schedule_PreviousOccurrence"] = "ocorrência anterior {0}",
                ["Schedule_NoNextOccurrence"] = "nenhuma próxima ocorrência no intervalo selecionado",
                ["Schedule_NoPreviousOccurrence"] = "nenhuma ocorrência anterior no intervalo selecionado",
                ["Relative_Now"] = "agora",
                ["Relative_In"] = "em {0}",
                ["Relative_Ago"] = "há {0}",
                ["Relative_Days"] = "{0} dia(s)",
                ["Relative_Hours"] = "{0} hora(s)",
                ["Relative_Minutes"] = "{0} minuto(s)",
                ["Relative_Seconds"] = "{0} segundo(s)"
            },
            ["ru"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "каждую минуту",
                ["Expression_EveryHourAtMinute"] = "каждый час в минуту {0}",
                ["Expression_EveryDayAtTime"] = "каждый день в {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "каждый месяц в день {0} в {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "каждый год {1} {0} в {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "каждую неделю в {0} в {1}",
                ["Expression_WeekdaysAtTime"] = "каждый будний день в {0}",
                ["Expression_WeekendsAtTime"] = "каждый выходной день в {0}",
                ["Expression_Generic"] = "расписание: {0}",
                ["Schedule_TimeZone"] = "Часовой пояс: {0}",
                ["Schedule_NextOccurrence"] = "следующее выполнение {0}",
                ["Schedule_PreviousOccurrence"] = "предыдущее выполнение {0}",
                ["Schedule_NoNextOccurrence"] = "нет следующего выполнения в выбранном интервале",
                ["Schedule_NoPreviousOccurrence"] = "нет предыдущего выполнения в выбранном интервале",
                ["Relative_Now"] = "сейчас",
                ["Relative_In"] = "через {0}",
                ["Relative_Ago"] = "{0} назад",
                ["Relative_Days"] = "{0} дн.",
                ["Relative_Hours"] = "{0} ч.",
                ["Relative_Minutes"] = "{0} мин.",
                ["Relative_Seconds"] = "{0} сек."
            },
            ["tr"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "her dakika",
                ["Expression_EveryHourAtMinute"] = "her saat {0}. dakikada",
                ["Expression_EveryDayAtTime"] = "her gün saat {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "her ayın {0}. günü saat {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "her yıl {0} ayının {1}. günü saat {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "her hafta {0} günü saat {1}",
                ["Expression_WeekdaysAtTime"] = "her hafta içi gün saat {0}",
                ["Expression_WeekendsAtTime"] = "her hafta sonu günü saat {0}",
                ["Expression_Generic"] = "zamanlama: {0}",
                ["Schedule_TimeZone"] = "Saat dilimi: {0}",
                ["Schedule_NextOccurrence"] = "sonraki oluşum {0}",
                ["Schedule_PreviousOccurrence"] = "önceki oluşum {0}",
                ["Schedule_NoNextOccurrence"] = "seçilen aralıkta sonraki oluşum yok",
                ["Schedule_NoPreviousOccurrence"] = "seçilen aralıkta önceki oluşum yok",
                ["Relative_Now"] = "şimdi",
                ["Relative_In"] = "{0} içinde",
                ["Relative_Ago"] = "{0} önce",
                ["Relative_Days"] = "{0} gün",
                ["Relative_Hours"] = "{0} saat",
                ["Relative_Minutes"] = "{0} dakika",
                ["Relative_Seconds"] = "{0} saniye"
            },
            ["uk"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Expression_EveryMinute"] = "щохвилини",
                ["Expression_EveryHourAtMinute"] = "щогодини на хвилині {0}",
                ["Expression_EveryDayAtTime"] = "щодня о {0}",
                ["Expression_EveryMonthOnDayAtTime"] = "щомісяця в день {0} о {1}",
                ["Expression_EveryYearOnMonthDayAtTime"] = "щороку {1} {0} о {2}",
                ["Expression_EveryWeekOnDayAtTime"] = "щотижня у {0} о {1}",
                ["Expression_WeekdaysAtTime"] = "щодня у будній день о {0}",
                ["Expression_WeekendsAtTime"] = "щодня у вихідний день о {0}",
                ["Expression_Generic"] = "розклад: {0}",
                ["Schedule_TimeZone"] = "Часовий пояс: {0}",
                ["Schedule_NextOccurrence"] = "наступне виконання {0}",
                ["Schedule_PreviousOccurrence"] = "попереднє виконання {0}",
                ["Schedule_NoNextOccurrence"] = "немає наступного виконання у вибраному інтервалі",
                ["Schedule_NoPreviousOccurrence"] = "немає попереднього виконання у вибраному інтервалі",
                ["Relative_Now"] = "зараз",
                ["Relative_In"] = "через {0}",
                ["Relative_Ago"] = "{0} тому",
                ["Relative_Days"] = "{0} дн.",
                ["Relative_Hours"] = "{0} год.",
                ["Relative_Minutes"] = "{0} хв.",
                ["Relative_Seconds"] = "{0} сек."
            }
        };

    /// <summary>
    /// Gets the localized value for a resource key using the requested language, with fallback to English.
    /// </summary>
    /// <remarks>Uses a resolved language before lookup and falls back to the English resource set when the
    /// key is not present in that language.</remarks>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="language">The requested language code.</param>
    /// <returns>The localized value for the specified key in the resolved language, or the English value when no 
    /// localized value is found.</returns>
    internal static string Get(string key, string language)
    {
        var resolvedLanguage = ResolveLanguage(language);

        if (_languages[resolvedLanguage].TryGetValue(key, out var value))
        {
            return value;
        }

        return _languages["en"][key];
    }

    /// <summary>
    /// Formats a localized string for the specified key and language using the supplied arguments.
    /// </summary>
    /// <param name="key">Resource key of the localized format string.</param>
    /// <param name="language">Language identifier used to resolve the culture and localized text.</param>
    /// <param name="args">Objects to format into the localized format string.</param>
    /// <returns>A culture-aware, localized string with the format items replaced by the supplied arguments.</returns>
    internal static string Format(string key, string language, params object[] args)
    {
        return string.Format(GetCulture(language), Get(key, language), args);
    }

    /// <summary>
    /// Gets a culture for the resolved language code.
    /// </summary>
    /// <param name="language">The input language identifier to resolve.</param>
    /// <returns>A <see cref="CultureInfo"/> for the mapped language, or en-US when the language is not supported.</returns>
    internal static CultureInfo GetCulture(string language)
    {
        return ResolveLanguage(language) switch
        {
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "es" => CultureInfo.GetCultureInfo("es-ES"),
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            "it" => CultureInfo.GetCultureInfo("it-IT"),
            "pt" => CultureInfo.GetCultureInfo("pt-PT"),
            "ru" => CultureInfo.GetCultureInfo("ru-RU"),
            "tr" => CultureInfo.GetCultureInfo("tr-TR"),
            "uk" => CultureInfo.GetCultureInfo("uk-UA"),
            _    => CultureInfo.GetCultureInfo("en-US")
        };
    }

    /// <summary>
    /// Resolves a language code to a supported value.
    /// </summary>
    /// <param name="language">The requested language code.</param>
    /// <returns>The specified language code when it is non-empty and supported; otherwise, <c>"en"</c>.</returns>
    internal static string ResolveLanguage(string language)
    {
        if (!string.IsNullOrWhiteSpace(language) && _languages.ContainsKey(language))
        {
            return language;
        }

        return "en";
    }
}
