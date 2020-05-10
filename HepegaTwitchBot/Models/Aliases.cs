namespace HepegaTwitchBot
{
    public static class Aliases
    {
        public static string[] AnfisaAliases =
        {
            "батл",
            "баттл",
            "раунд",
            "рэп",
            "реп"
        };

        public static string[] MelAliases =
        {
            "mel",
            "melharucos",
            "мэл",
            "мел",
            "кубоеб"
        };
        public static string[] ProhibitedAliases =
        {
            " Champ",
            "taxibro",
            "DICKS",
            "пид",
            "ПИДOР",
            "нигер",
            "Hигер",
            "нигeр",
            "негр",
            "нeгр",
            "ПИДОР",
            "пидор",
            "пnдор",
            "дарас",
            "дaрас",
            "дaрac",
            "даpас",
            "пид0р",
            "пид0p",
            "пидop",
            "пидоp",
            "pidor",
            "pid0r",
            "пидер"
        };
        public static string[] UzyaAliases =
        {
            "юз",
            "us",
            "uz",
            "uselessmouth",
            "юзя",
            "гений",
            "зюзя",
            "useless"
        };
        public static string[] FakerAliases =
        {
            "фак",
            "fak",
            "mistafaker",
            "faker",
            "факер",
            "лепрекон"
        };
        public static string[] BjornAliases =
        {
            "бурн",
            "бь",
            "бъ",
            "unclebjorn",
            "bjorn",
            "бьерн",
            "бьорн",
            "анклбьерн",
            "анкл",
            "медведь"
        };
        public static string[] LasqaAliases =
        {
            "las",
            "lasqa",
            "ласка",
            "ласочка",
            "крыса"
        };
        public static string[] LizonAliases =
        {
            "лиз",
            "liz",
            "liz0n",
            "лизон",
            "девочка",
            "тян"
        };

        public static bool ContainsAlias(this string message, string[] aliasses)
        {
            message = message.ToLower();
            foreach (var alias in aliasses)
            {
                if (message.Contains(alias.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}