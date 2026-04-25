using System.Globalization;
using System.Text;

namespace Greenhouse.Application.Voice;

/// <summary>
/// Konwersja liczb całkowitych na polskie słowa (mianownik) — żeby wypowiedzi głosowe miały
/// jednoznaczne brzmienie niezależnie od silnika TTS (Web Speech API w przeglądarce ma różną
/// jakość czytania liczb pisanych cyframi).
///
/// Zakres: 0 – 9999 (wystarczający dla minut/dni; 9999 minut to ~7 dni — dłuższe okresy
/// raportujemy w innych jednostkach).
/// </summary>
public static class PolishNumberWords
{
    private static readonly string[] Ones =
    {
        "zero", "jeden", "dwa", "trzy", "cztery", "pięć", "sześć", "siedem", "osiem", "dziewięć",
        "dziesięć", "jedenaście", "dwanaście", "trzynaście", "czternaście", "piętnaście",
        "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście"
    };

    private static readonly string[] Tens =
    {
        "", "", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
        "sześćdziesiąt", "siedemdziesiąt", "osiemdziesiąt", "dziewięćdziesiąt"
    };

    private static readonly string[] Hundreds =
    {
        "", "sto", "dwieście", "trzysta", "czterysta", "pięćset",
        "sześćset", "siedemset", "osiemset", "dziewięćset"
    };

    /// <summary>
    /// Słownie po polsku w mianowniku. Liczby ujemne dostają prefiks „minus”.
    /// Powyżej 9999 zwraca cyfry sformatowane invariantem (graceful fallback).
    /// </summary>
    public static string ToPolishWords(int value)
    {
        if (value < 0)
            return "minus " + ToPolishWords(-value);
        if (value > 9999)
            return value.ToString(CultureInfo.InvariantCulture);
        if (value < 20)
            return Ones[value];
        if (value < 100)
        {
            var tens = value / 10;
            var ones = value % 10;
            return ones == 0
                ? Tens[tens]
                : Tens[tens] + " " + Ones[ones];
        }
        if (value < 1000)
        {
            var hundreds = value / 100;
            var rest = value % 100;
            return rest == 0
                ? Hundreds[hundreds]
                : Hundreds[hundreds] + " " + ToPolishWords(rest);
        }

        var thousands = value / 1000;
        var remainder = value % 1000;
        var thousandWord = ThousandWord(thousands);
        var thousandsPart = thousands == 1 ? thousandWord : ToPolishWords(thousands) + " " + thousandWord;
        return remainder == 0
            ? thousandsPart
            : thousandsPart + " " + ToPolishWords(remainder);
    }

    /// <summary>
    /// Forma "minut" zgodna z liczbą (mianownik liczebnika): „jedna minuta”, „dwie minuty”,
    /// „pięć minut”, „dwadzieścia jeden minut”. Liczba pisana słownie.
    /// </summary>
    public static string MinutesPhrase(int n)
    {
        if (n < 0) n = 0;
        if (n == 1)
            return "jedna minuta";
        // „dwie/trzy/cztery minuty” — w mianowniku liczebnika dwa→dwie dla rodzaju żeńskiego
        if (n == 2) return "dwie minuty";
        if (n == 3) return "trzy minuty";
        if (n == 4) return "cztery minuty";

        var word = MinuteWordForCount(n);
        return ToPolishWords(n) + " " + word;
    }

    /// <summary>„dni” / „dzień” / „dni” w zależności od liczby.</summary>
    public static string DaysPhrase(int n)
    {
        if (n < 0) n = 0;
        if (n == 1) return "jeden dzień";
        return ToPolishWords(n) + " dni";
    }

    private static string MinuteWordForCount(int n)
    {
        var mod10 = Math.Abs(n) % 10;
        var mod100 = Math.Abs(n) % 100;
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
            return "minuty";
        return "minut";
    }

    private static string ThousandWord(int thousands)
    {
        if (thousands == 1) return "tysiąc";
        var mod10 = thousands % 10;
        var mod100 = thousands % 100;
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
            return "tysiące";
        return "tysięcy";
    }
}
