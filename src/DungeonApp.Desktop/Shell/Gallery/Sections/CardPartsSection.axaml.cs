using Avalonia.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

/// <summary>
/// Sample entries for the gallery's prose sections: one with an introduction, a note and emphasis,
/// one without an introduction, a name that already ends in punctuation (no period added) and a
/// stray <c>**</c> that stays in the text as written. Then a paragraph opened by an initial, one long
/// enough to flow under the icon and one that stays beside it.
/// </summary>
public partial class CardPartsSection : UserControl
{
    public CardPartsSection()
    {
        InitializeComponent();

        ProseWithIntro.Intro = "Może wykonać **3 akcje legendarne** na turę, wybierając spośród poniższych. Na początku swojej tury odzyskuje wydane akcje.";
        ProseWithIntro.Items =
        [
            new ProseItem("Wykrycie", null, "Wykonuje test **Mądrości (Percepcja)**."),
            new ProseItem("Uderzenie ogonem", "kosztuje 2 akcje", "Atak w zwarciu: **+7 do trafienia**, zasięg 3 m, jeden cel. Trafienie: **10 (2k6+3)** obrażeń obuchowych."),
        ];

        ProseWithoutIntro.Items =
        [
            new ProseItem("Taktyka gromady", "raz na turę", "Ma ułatwienie w rzucie ataku, jeśli w promieniu 1,5 m od celu stoi sojusznik."),
            new ProseItem("Czujny!", null, "Niesparowany znacznik ** zostaje w tekście tak, jak go zapisano."),
        ];

        InitialLong.Text = "Pierwsze dwie linie tego akapitu zaczynają się za ikoną, która stoi w lewym górnym rogu jak inicjał w rękopisie. "
                           + "Od trzeciej linii tekst wraca do lewej krawędzi i płynie dalej **tym samym rytmem**, bez odstępu na styku. "
                           + "Gdy szerokość się zmienia, podział wypada zawsze na końcu drugiej linii, a słowo nigdy się nie rozrywa.\n"
                           + "Twardy koniec wiersza też liczy się jako koniec linii.";
        InitialShort.Text = "Krótki akapit mieści się obok ikony.";
    }
}
