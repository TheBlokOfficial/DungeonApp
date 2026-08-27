# Źródła zasobów Desktop

Zasoby są przechowywane lokalnie i aplikacja nie pobiera ich podczas działania.

## Alegreya

- Plik: `Fonts/Alegreya/Alegreya-VariableFont_wght.ttf`
- Rodzina: Alegreya
- Źródło: `google/fonts`, katalog `ofl/alegreya`
- Commit: `ec626514f79f831f1ab848a82114a0ce7e2d6372`
- Licencja: SIL Open Font License 1.1
- Kopia licencji: `Licenses/Alegreya-OFL-1.1.txt`

## Lucide

- Zakres: wyłącznie SVG używane lub przewidziane przez podstawowy shell
- Źródło: `lucide-icons/lucide`, katalog `icons`
- Commit: `23f9abc4ed0146cffededd3d7f94c1018bfdf693`
- Licencja: ISC z notą MIT dla ikon wywodzących się z Feather
- Kopia licencji: `Licenses/Lucide-ISC.txt`

Źródłowe SVG są zachowane jako punkt audytu. Produkcyjny interfejs Avalonia używa odpowiadających im wektorowych `DrawingImage`, ponieważ podstawowy loader Avalonia nie interpretuje SVG bez dodatkowej biblioteki runtime.

`Icons/panel-dock-bottom.svg`, `Icons/window-maximize.svg` i `Icons/window-restore.svg` są
ikonami własnymi projektu, narysowanymi w konwencji wizualnej Lucide. Pierwsza łączy dolny
pasek i skierowany w dół chevron, aby opisać lokalną akcję minimalizacji modułu. Pozostałe
dwie przedstawiają standardowe stany maksymalizacji i przywracania pływającego panelu.
